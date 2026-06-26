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
    private readonly AppDbContext _db;
    private readonly MakorScraperService _scraper;

    public SyncService(AppDbContext db, MakorScraperService scraper)
    {
        _db = db;
        _scraper = scraper;
    }

    public async Task<SyncResponse> ExecuteSync(List<string> terms)
    {
        var provider = await _db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "makor");
        if (provider == null) return new SyncResponse(false, 0);

        var log = new SyncLog { ProveedorId = provider.Id, Estado = EstadoSync.EN_PROCESO };
        _db.SyncLogs.Add(log);
        await _db.SaveChangesAsync();

        int totalUpserted = 0;
        int errors = 0;
        var errorDetails = new List<string>();

        try
        {
            var makorUser = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == "makor_user");
            var makorPass = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == "makor_pass");
            string user = makorUser?.Valor ?? "12906";
            string pass = makorPass?.Valor ?? "cacere";
            await _scraper.LoginAsync(user, pass);

            var allCategories = await _db.Categorias.ToListAsync();
            var allSubcategories = await _db.Subcategorias.ToListAsync();

            foreach (var term in terms)
            {
                // Si el term corresponde a un slug de categoría, usamos la categoría, sino la búsqueda.
                // Como pasamos el slug desde el frontend, podemos usar GetProductsByCategory directamente si coincide con algún slug nuestro o si es un slug típico.
                // Para ser seguros, si tiene espacios, es búsqueda libre. Si tiene guiones, asumimos slug de categoría de makor.
                List<MakorProductScraped> scrapedProducts;
                if (term.Contains("-") && !term.Contains(" "))
                {
                    scrapedProducts = await _scraper.GetProductsByCategory(term);
                }
                else
                {
                    scrapedProducts = await _scraper.SearchProducts(term);
                }

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
                        }

                        var prod = await _db.Productos.FirstOrDefaultAsync(p => p.CodigoMakor == scraped.CodigoMakor);
                        if (prod == null)
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
                                PrecioMinorista = scraped.Precio, // Por ahora el mismo
                                UltimaSync = DateTime.UtcNow
                            };
                            _db.Productos.Add(prod);
                            log.ProductosNuevos++;
                        }
                        else
                        {
                            if (scraped.Precio.HasValue)
                            {
                                prod.PrecioMayorista = scraped.Precio;
                                prod.PrecioMinorista = scraped.Precio; // Considerar regla de negocio futura
                            }
                            prod.UltimaSync = DateTime.UtcNow;
                            prod.UpdatedAt = DateTime.UtcNow;
                            log.ProductosActualizados++;
                        }
                        await _db.SaveChangesAsync();
                        totalUpserted++;

                        if (!string.IsNullOrEmpty(scraped.ImagenUrl))
                        {
                            var img = await _db.ProductoImagenes.FirstOrDefaultAsync(i => i.ProductoId == prod.Id && i.UrlOriginal == scraped.ImagenUrl);
                            if (img == null)
                            {
                                img = new ProductoImagen
                                {
                                    ProductoId = prod.Id,
                                    UrlOriginal = scraped.ImagenUrl,
                                    EsPrincipal = true
                                };
                                _db.ProductoImagenes.Add(img);
                                await _db.SaveChangesAsync();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        errors++;
                        errorDetails.Add($"Error on {scraped.CodigoMakor}: {ex.Message}");
                    }
                }
            }

            provider.UltimaSync = DateTime.UtcNow;
            log.Estado = errors == 0 ? EstadoSync.COMPLETADO : (totalUpserted > 0 ? EstadoSync.COMPLETADO : EstadoSync.ERROR);
            log.Errores = errors;
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
            log.DetalleErrores = JsonSerializer.Serialize(new[] { ex.Message });
            await _db.SaveChangesAsync();
            return new SyncResponse(false, 0);
        }
    }

    private string NormalizeName(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var textWithSpaces = text.Replace("-", " ");
        var textInfo = new CultureInfo("es-AR", false).TextInfo;
        return textInfo.ToTitleCase(textWithSpaces);
    }
}
