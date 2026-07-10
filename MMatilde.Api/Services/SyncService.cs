using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.Helpers;
using MMatilde.Api.Models;
using MMatilde.Api.DTOs;
using System.Text.Json;
using System.Globalization;

namespace MMatilde.Api.Services;

public class SyncService
{
    private sealed class SyncTermResumen
    {
        public string Term { get; set; } = string.Empty;
        public bool EsCategoria { get; set; }
        public int ProductosEncontrados { get; set; }
        public int ProductosNuevos { get; set; }
        public int ProductosActualizados { get; set; }
        public int Errores { get; set; }
    }

    private readonly AppDbContext _db;
    private readonly MakorScraperService _scraper;
    private readonly PricingService _pricing;

    public SyncService(AppDbContext db, MakorScraperService scraper, PricingService pricing)
    {
        _db = db;
        _scraper = scraper;
        _pricing = pricing;
    }

    public async Task<SyncResponse> ExecuteSync(List<string> terms)
    {
        var provider = await _db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "makor");
        if (provider == null) return new SyncResponse(false, 0);

        var cleanTerms = terms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var log = new SyncLog
        {
            ProveedorId = provider.Id,
            Estado = EstadoSync.EN_PROCESO,
            TermsJson = JsonSerializer.Serialize(cleanTerms)
        };
        _db.SyncLogs.Add(log);
        await _db.SaveChangesAsync();

        int totalUpserted = 0;
        int errors = 0;
        var errorDetails = new List<string>();
        var categoriasAfectadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resumenPorTermino = new List<SyncTermResumen>();

        try
        {
            var makorUser = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == "makor_user");
            var makorPass = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == "makor_pass");
            string user = makorUser?.Valor ?? "12906";
            string pass = makorPass?.Valor ?? "cacere";
            await _scraper.LoginAsync(user, pass);

            var allCategories = await _db.Categorias.ToListAsync();
            var allSubcategories = await _db.Subcategorias.ToListAsync();

            foreach (var term in cleanTerms)
            {
                // Si el term corresponde a un slug de categoría, usamos la categoría, sino la búsqueda.
                // Como pasamos el slug desde el frontend, podemos usar GetProductsByCategory directamente si coincide con algún slug nuestro o si es un slug típico.
                // Para ser seguros, si tiene espacios, es búsqueda libre. Si tiene guiones, asumimos slug de categoría de makor.
                List<MakorProductScraped> scrapedProducts;
                var esCategoria = term.Contains("-") && !term.Contains(" ");
                if (esCategoria)
                {
                    scrapedProducts = await _scraper.GetProductsByCategory(term);
                }
                else
                {
                    scrapedProducts = await _scraper.SearchProducts(term);
                }

                var resumenTerm = new SyncTermResumen
                {
                    Term = term,
                    EsCategoria = esCategoria,
                    ProductosEncontrados = scrapedProducts.Count
                };
                resumenPorTermino.Add(resumenTerm);

                foreach (var scraped in scrapedProducts)
                {
                    try
                    {
                        var catName = NormalizeName(scraped.CategoriaSlug);
                        var catSlug = SlugHelper.Slugify(catName);

                        var cat = allCategories.FirstOrDefault(c => c.Slug == catSlug);
                        if (cat == null)
                        {
                            cat = new Categoria { Nombre = catName, Slug = catSlug };
                            _db.Categorias.Add(cat);
                            await _db.SaveChangesAsync();
                            allCategories.Add(cat);
                        }

                        categoriasAfectadas.Add(cat.Nombre);

                        Subcategoria? subcat = null;
                        if (!string.IsNullOrEmpty(scraped.SubcategoriaSlug))
                        {
                            var subName = NormalizeName(scraped.SubcategoriaSlug);
                            var subSlug = SlugHelper.Slugify(subName);

                            subcat = allSubcategories.FirstOrDefault(s => s.Slug == subSlug && s.CategoriaId == cat.Id);
                            if (subcat == null)
                            {
                                subcat = new Subcategoria { Nombre = subName, Slug = subSlug, CategoriaId = cat.Id };
                                _db.Subcategorias.Add(subcat);
                                await _db.SaveChangesAsync();
                                allSubcategories.Add(subcat);
                            }

                            categoriasAfectadas.Add($"{cat.Nombre} > {subcat.Nombre}");
                        }

                        var prod = await _db.Productos
                            .Include(p => p.Presentaciones)
                            .FirstOrDefaultAsync(p => p.CodigoMakor == scraped.CodigoMakor);
                        var isNew = prod == null;
                        var nombreAnterior = prod?.Nombre;
                        if (isNew)
                        {
                            prod = new Producto
                            {
                                CodigoMakor = scraped.CodigoMakor,
                                Nombre = scraped.Nombre,
                                Slug = SlugHelper.Slugify(scraped.Nombre + "-" + scraped.CodigoMakor),
                                CategoriaId = cat.Id,
                                SubcategoriaId = subcat?.Id,
                                ProveedorId = provider.Id,
                                PrecioMayorista = scraped.Precio,
                                PrecioMinorista = scraped.Precio,
                                UltimaSync = DateTime.UtcNow
                            };
                            _db.Productos.Add(prod);
                            log.ProductosNuevos++;
                            resumenTerm.ProductosNuevos++;
                        }
                        else
                        {
                            prod!.Nombre = scraped.Nombre;
                            if (scraped.Precio.HasValue && prod.ModoPrecio != ModoPrecio.PRECIO_FIJO)
                            {
                                prod.PrecioMayorista = scraped.Precio;
                                if (!prod.Presentaciones.Any(p => p.Activo))
                                    prod.PrecioMinorista = scraped.Precio;
                            }
                            prod.UltimaSync = DateTime.UtcNow;
                            prod.UpdatedAt = DateTime.UtcNow;
                            log.ProductosActualizados++;
                            resumenTerm.ProductosActualizados++;
                        }

                        MakorPublicContent.ApplySyncedPublicFields(prod, scraped.Nombre, nombreAnterior, isNew);

                        if (prod.UnidadBase == null || prod.UnidadCompraAutoDetectada)
                            AplicarUnidadDetectada(prod, scraped.Nombre);

                        await _pricing.EnsurePresentacionVentaListaAsync(prod);
                        await _db.SaveChangesAsync();
                        totalUpserted++;

                        if (!string.IsNullOrEmpty(scraped.ImagenUrl))
                        {
                            var img = await _db.ProductoImagenes
                                .FirstOrDefaultAsync(i => i.ProductoId == prod.Id && i.EsDeProveedor);
                            if (img == null)
                            {
                                img = new ProductoImagen
                                {
                                    ProductoId = prod.Id,
                                    UrlOriginal = scraped.ImagenUrl,
                                    EsPrincipal = true,
                                    EsDeProveedor = true
                                };
                                _db.ProductoImagenes.Add(img);
                            }
                            else
                            {
                                img.UrlOriginal = scraped.ImagenUrl;
                            }
                            await _db.SaveChangesAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        resumenTerm.Errores++;
                        errorDetails.Add($"Error on {scraped.CodigoMakor}: {ex.Message}");
                    }
                }
            }

            provider.UltimaSync = DateTime.UtcNow;
            log.Estado = errors == 0 ? EstadoSync.COMPLETADO : (totalUpserted > 0 ? EstadoSync.COMPLETADO : EstadoSync.ERROR);
            log.Errores = errors;
            log.CategoriasJson = JsonSerializer.Serialize(categoriasAfectadas.OrderBy(x => x).ToList());
            log.ResumenJson = JsonSerializer.Serialize(resumenPorTermino);
            log.FinalizadoEn = DateTime.UtcNow;
            if (errorDetails.Count > 0)
            {
                log.DetalleErrores = JsonSerializer.Serialize(errorDetails);
            }

            await _db.SaveChangesAsync();
            return new SyncResponse(true, totalUpserted);
        }
        catch (Exception ex)
        {
            log.Estado = EstadoSync.ERROR;
            log.FinalizadoEn = DateTime.UtcNow;
            log.CategoriasJson = JsonSerializer.Serialize(categoriasAfectadas.OrderBy(x => x).ToList());
            log.ResumenJson = JsonSerializer.Serialize(resumenPorTermino);
            log.DetalleErrores = JsonSerializer.Serialize(new[] { ex.Message });
            await _db.SaveChangesAsync();
            return new SyncResponse(false, 0);
        }
    }

    private static void AplicarUnidadDetectada(Producto prod, string nombre) =>
        UnidadParser.TryApplyTo(prod, nombre);

    private string NormalizeName(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var textWithSpaces = text.Replace("-", " ");
        var textInfo = new CultureInfo("es-AR", false).TextInfo;
        return textInfo.ToTitleCase(textWithSpaces);
    }
}
