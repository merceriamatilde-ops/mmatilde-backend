using MMatilde.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;
using MMatilde.Api.Helpers;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[Route("api/productos")]
[ApiController]
public class ProductosController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PricingService _pricing;
    private readonly SyncService _sync;

    public ProductosController(AppDbContext db, PricingService pricing, SyncService sync)
    {
        _db = db;
        _pricing = pricing;
        _sync = sync;
    }

    [HttpGet]
    [Authorize]
    [AuthorizeModule("productos")]
    public async Task<ActionResult<ProductoAdminListResponse>> Get(
        [FromQuery] string? q, 
        [FromQuery] int? categoriaId, 
        [FromQuery] int? subcategoriaId,
        [FromQuery] int? proveedorId,
        [FromQuery] int? tagId,
        [FromQuery] bool? activo,
        [FromQuery] bool? destacado,
        [FromQuery] bool sinPrecio = false,
        [FromQuery] bool sinImagen = false,
        [FromQuery] bool sinSync = false,
        [FromQuery] string? syncDesde = null,
        [FromQuery] string? syncHasta = null,
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 50)
    {
        var query = _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Subcategoria)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p => 
                EF.Functions.ILike(p.Nombre, $"%{q}%") || 
                (p.NombrePublico != null && EF.Functions.ILike(p.NombrePublico, $"%{q}%")) ||
                EF.Functions.ILike(p.CodigoMakor, $"%{q}%") ||
                (p.Categoria != null && EF.Functions.ILike(p.Categoria.Nombre, $"%{q}%")) ||
                (p.Subcategoria != null && EF.Functions.ILike(p.Subcategoria.Nombre, $"%{q}%")));
        }

        if (categoriaId.HasValue)
        {
            query = query.Where(p => p.CategoriaId == categoriaId);
        }
        if (subcategoriaId.HasValue)
        {
            query = query.Where(p => p.SubcategoriaId == subcategoriaId);
        }
        if (proveedorId.HasValue)
        {
            query = query.Where(p => p.ProveedorId == proveedorId);
        }
        if (tagId.HasValue)
        {
            query = query.Where(p => p.Tags.Any(t => t.TagId == tagId.Value));
        }
        if (activo.HasValue)
        {
            query = query.Where(p => p.Activo == activo);
        }
        if (destacado.HasValue)
        {
            query = query.Where(p => p.Destacado == destacado.Value);
        }
        if (sinPrecio)
        {
            query = query.Where(p =>
                p.PrecioMinorista == null &&
                !p.Presentaciones.Any(pr => pr.Activo && pr.PrecioVenta != null) &&
                (p.ModoPrecio != ModoPrecio.AUTOMATICO || p.PrecioMayorista == null));
        }
        if (sinImagen)
        {
            query = query.Where(p =>
                (p.ImagenPublicaUrl == null || p.ImagenPublicaUrl == "") &&
                !p.Imagenes.Any());
        }
        if (sinSync)
        {
            query = query.Where(p => p.UltimaSync == null);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(syncDesde) && DateOnly.TryParse(syncDesde, out var desde))
            {
                var (desdeUtc, _) = VentasService.RangoDiaArgentina(desde);
                query = query.Where(p => p.UltimaSync != null && p.UltimaSync >= desdeUtc);
            }
            if (!string.IsNullOrWhiteSpace(syncHasta) && DateOnly.TryParse(syncHasta, out var hasta))
            {
                var (_, hastaUtc) = VentasService.RangoDiaArgentina(hasta);
                query = query.Where(p => p.UltimaSync != null && p.UltimaSync <= hastaUtc);
            }
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)pageSize);

        var products = await query
            .Include(p => p.Presentaciones)
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<ProductoAdminDto>(products.Count);
        foreach (var p in products)
        {
            if (p.UnidadBase == null || !p.CantidadUnidadCompra.HasValue || p.CantidadUnidadCompra <= 0 ||
                p.UnidadCompraAutoDetectada)
                UnidadParser.ApplyDetectedOrDefault(p, p.Nombre);
            _pricing.EnsurePresentacionVentaDefault(p);

            var venta = await _pricing.ResolverPrecioVentaDefaultAsync(p);
            items.Add(new ProductoAdminDto(
                p.Id,
                p.CodigoMakor,
                p.Nombre,
                p.NombrePublico,
                ProductoDisplay.NombrePublico(p),
                p.Slug,
                p.Categoria != null ? (p.Subcategoria != null ? p.Categoria.Nombre + " > " + p.Subcategoria.Nombre : p.Categoria.Nombre) : "",
                p.PrecioMayorista,
                p.PrecioMinorista,
                venta.PrecioVentaFinal,
                venta.PresentacionNombre,
                p.Activo,
                p.Destacado,
                p.UltimaSync,
                p.ModoOrigenEconomico.ToString(),
                p.ModoPrecio.ToString(),
                p.ProveedorId,
                p.EsVentaLibre
            ));
        }

        return new ProductoAdminListResponse(items, total, page, pageSize, totalPages);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<ProductoDetalleDto>> GetBySlug(string slug)
    {
        var prod = await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Subcategoria)
            .Include(p => p.Imagenes)
            .Include(p => p.Variantes).ThenInclude(v => v.Color)
            .Include(p => p.Relacionados).ThenInclude(r => r.ProductoVinculado).ThenInclude(pv => pv.Imagenes)
            .Include(p => p.Tags).ThenInclude(t => t.Tag)
            .FirstOrDefaultAsync(p => p.Slug == slug && p.Activo && !p.EsVentaLibre);

        if (prod == null) return NotFound();

        var imgUrls = ProductoDisplay.ImagenesPublicas(prod);

        var variantesDto = prod.Variantes?.Where(v => v.Activo).OrderBy(v => v.Orden).Select(v => new VarianteResponseDto(
            v.Id,
            v.ColorId,
            v.Color?.Nombre,
            v.Color?.CodigoHex,
            v.Talle,
            v.Medida,
            v.CodigoArticulo,
            v.Activo
        )).ToList();

        var relacionadosDto = prod.Relacionados?
            .Where(r => r.ProductoVinculado.Activo)
            .Select(r => new ProductoRelacionadoDto(
            r.ProductoVinculado.Id,
            ProductoDisplay.NombrePublico(r.ProductoVinculado),
            r.ProductoVinculado.Slug,
            ProductoDisplay.ImagenPublica(r.ProductoVinculado)
        )).ToList();

        var tagsDto = prod.Tags?
            .Where(t => t.Tag.Activo)
            .OrderBy(t => t.Tag.Orden)
            .Select(t => new TagResumenDto(t.Tag.Id, t.Tag.Nombre, t.Tag.Slug))
            .ToList();

        return new ProductoDetalleDto(
            prod.Id,
            prod.Slug,
            ProductoDisplay.NombrePublico(prod),
            ProductoDisplay.DescripcionPublica(prod),
            prod.Categoria?.Nombre ?? "",
            prod.Categoria?.Slug ?? "",
            prod.Subcategoria?.Nombre,
            prod.Subcategoria?.Slug,
            imgUrls,
            variantesDto,
            relacionadosDto,
            tagsDto
        );
    }

    [HttpPut("{id}/toggle-activo")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ToggleActivo(int id, [FromBody] ToggleRequest req)
    {
        var prod = await _db.Productos.FindAsync(id);
        if (prod == null) return NotFound();

        if (prod.EsVentaLibre)
            return BadRequest(new { message = "El producto de venta libre no puede mostrarse en el catálogo." });

        prod.Activo = req.Value;
        prod.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("{id}/toggle-destacado")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ToggleDestacado(int id, [FromBody] ToggleRequest req)
    {
        var prod = await _db.Productos.FindAsync(id);
        if (prod == null) return NotFound();

        prod.Destacado = req.Value;
        prod.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { success = true });
    }

    [HttpPut("bulk-toggle")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> BulkToggle([FromBody] BulkToggleRequest req)
    {
        var prod = await _db.Productos.Where(p => req.Ids.Contains(p.Id) && !p.EsVentaLibre).ToListAsync();
        foreach (var p in prod)
        {
            p.Activo = req.Activo;
            p.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();

        return Ok(new { success = true, count = prod.Count });
    }

    /// <summary>Solo desarrollo: activa todo el catálogo para pruebas de IA / catálogo.</summary>
    [HttpPut("dev/activate-all")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> ActivateAllDev([FromServices] IWebHostEnvironment env)
    {
        if (!env.IsDevelopment()) return NotFound();

        var count = await _db.Productos.ExecuteUpdateAsync(p => p.SetProperty(x => x.Activo, true));
        return Ok(new { success = true, count });
    }

    // Productos que tienen variantes, para el "Copiar variantes de..." del modal.
    [HttpGet("con-variantes")]
    [Authorize]
    [AuthorizeModule("productos")]
    public async Task<ActionResult<List<ProductoConVariantesDto>>> GetConVariantes(
        [FromQuery] string? q,
        [FromQuery] int? excluirId,
        [FromQuery] int limit = 10)
    {
        var query = _db.Productos
            .Where(p => p.Variantes.Any())
            .AsQueryable();

        if (excluirId.HasValue)
            query = query.Where(p => p.Id != excluirId.Value);

        if (!string.IsNullOrWhiteSpace(q))
        {
            q = q.Trim();
            query = query.Where(p =>
                EF.Functions.ILike(p.Nombre, $"%{q}%") ||
                (p.NombrePublico != null && EF.Functions.ILike(p.NombrePublico, $"%{q}%")) ||
                EF.Functions.ILike(p.CodigoMakor, $"%{q}%"));
        }

        var rows = await query
            .OrderByDescending(p => p.Id)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(p => new { Producto = p, Count = p.Variantes.Count })
            .ToListAsync();

        return rows
            .Select(r => new ProductoConVariantesDto(
                r.Producto.Id,
                ProductoDisplay.NombrePublico(r.Producto),
                r.Producto.CodigoMakor,
                r.Count))
            .ToList();
    }

    // Variantes limpias de un producto, para copiar a otro.
    [HttpGet("{id}/variantes")]
    [Authorize]
    [AuthorizeModule("productos")]
    public async Task<ActionResult<List<VarianteCopiaDto>>> GetVariantes(int id)
    {
        var existe = await _db.Productos.AnyAsync(p => p.Id == id);
        if (!existe) return NotFound();

        var variantes = await _db.ProductoVariantes
            .Where(v => v.ProductoId == id)
            .OrderBy(v => v.Orden)
            .Select(v => new VarianteCopiaDto(
                v.ColorId,
                v.Color != null ? v.Color.Nombre : null,
                v.Talle,
                v.Medida,
                v.CodigoArticulo,
                v.Activo,
                v.Orden))
            .ToListAsync();

        return variantes;
    }

    [HttpGet("admin/{id}")]
    [Authorize]
    [AuthorizeModule("productos")]
    public async Task<ActionResult<object>> GetById(int id)
    {
        var prod = await _db.Productos
            .Include(p => p.Imagenes)
            .Include(p => p.Variantes)
            .Include(p => p.Relacionados).ThenInclude(r => r.ProductoVinculado)
            .Include(p => p.Tags).ThenInclude(t => t.Tag)
            .Include(p => p.Proveedor)
            .FirstOrDefaultAsync(p => p.Id == id);
        
        if (prod == null) return NotFound();

        return new {
            prod.Id,
            prod.CodigoMakor,
            prod.Nombre,
            nombrePublico = prod.NombrePublico,
            prod.Slug,
            prod.Descripcion,
            descripcionPublica = prod.DescripcionPublica,
            imagenPublicaUrl = prod.ImagenPublicaUrl,
            imagenProveedorUrl = ProductoDisplay.ImagenProveedor(prod),
            prod.Composicion,
            prod.PrecioMayorista,
            prod.PrecioMinorista,
            prod.DescuentoPorcentaje,
            prod.Destacado,
            prod.Activo,
            prod.CategoriaId,
            prod.SubcategoriaId,
            prod.MarcaId,
            prod.ProveedorId,
            prod.UltimaSync,
            prod.Imagenes,
            prod.Variantes,
            Relacionados = prod.Relacionados.Select(r => new {
                id = r.ProductoVinculadoId,
                nombre = r.ProductoVinculado.Nombre,
                codigo = r.ProductoVinculado.CodigoMakor
            }),
            Tags = prod.Tags.OrderBy(t => t.Tag.Orden).Select(t => new {
                id = t.TagId,
                nombre = t.Tag.Nombre,
                slug = t.Tag.Slug
            })
        };
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Create([FromBody] ProductoCreateDto dto)
    {
        var prov = await _db.Proveedores.FirstOrDefaultAsync(p => p.Slug == "manual");
        if (prov == null) return BadRequest(new { message = "Proveedor 'Manual' no configurado en la BD." });

        var codigo = string.IsNullOrWhiteSpace(dto.Codigo) 
            ? "M-" + Guid.NewGuid().ToString().Substring(0, 6).ToUpper() 
            : dto.Codigo.Trim();

        if (await _db.Productos.AnyAsync(p => p.CodigoMakor == codigo))
            return BadRequest(new { message = "Ya existe un producto con ese código." });

        var slug = MMatilde.Api.Helpers.SlugHelper.Slugify(dto.Nombre + "-" + codigo);

        var p = new Models.Producto
        {
            Nombre = dto.Nombre,
            CodigoMakor = codigo,
            Slug = slug,
            CategoriaId = dto.CategoriaId,
            SubcategoriaId = dto.SubcategoriaId,
            Descripcion = dto.Descripcion,
            PrecioMayorista = dto.PrecioBase,
            PrecioMinorista = dto.PrecioBase * 1.21m * 1.70m, // Aprox
            Destacado = dto.Destacado,
            Activo = dto.Visible,
            ProveedorId = prov.Id
        };

        if (!string.IsNullOrEmpty(dto.ImagenUrl))
        {
            p.Imagenes.Add(new Models.ProductoImagen
            {
                UrlOriginal = dto.ImagenUrl,
                EsPrincipal = true,
                Orden = 0
            });
        }

        if (dto.Variantes != null)
        {
            foreach(var v in dto.Variantes)
            {
                p.Variantes.Add(new ProductoVariante {
                    ColorId = v.ColorId,
                    Talle = v.Talle,
                    Medida = v.Medida,
                    CodigoArticulo = v.CodigoArticulo,
                    Activo = v.Activo,
                    Orden = v.Orden
                });
            }
        }

        if (dto.RelacionadosIds != null)
        {
            foreach (var relId in dto.RelacionadosIds)
            {
                p.Relacionados.Add(new ProductoRelacionado { ProductoVinculadoId = relId });
            }
        }

        if (dto.TagIds != null)
        {
            foreach (var tagId in dto.TagIds.Distinct())
            {
                p.Tags.Add(new ProductoTag { TagId = tagId });
            }
        }

        _db.Productos.Add(p);
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Update(int id, [FromBody] ProductoUpdateDto dto)
    {
        var p = await _db.Productos
            .Include(pr => pr.Imagenes)
            .Include(pr => pr.Variantes)
            .Include(pr => pr.Relacionados)
            .Include(pr => pr.Tags)
            .Include(pr => pr.Proveedor)
            .FirstOrDefaultAsync(pr => pr.Id == id);

        if (p == null) return NotFound();

        var isMakor = p.Proveedor?.Slug == "makor";

        if (isMakor)
        {
            p.NombrePublico = string.IsNullOrWhiteSpace(dto.NombrePublico) ? null : dto.NombrePublico.Trim();
            p.DescripcionPublica = string.IsNullOrWhiteSpace(dto.DescripcionPublica) ? null : dto.DescripcionPublica.Trim();
            p.ImagenPublicaUrl = string.IsNullOrWhiteSpace(dto.ImagenPublicaUrl) ? null : dto.ImagenPublicaUrl.Trim();
            p.Destacado = dto.Destacado;
            p.Activo = dto.Visible;
            p.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var codigo = string.IsNullOrWhiteSpace(dto.Codigo) ? p.CodigoMakor : dto.Codigo.Trim();
            
            if (codigo != p.CodigoMakor && await _db.Productos.AnyAsync(x => x.CodigoMakor == codigo))
                return BadRequest(new { message = "Ya existe un producto con ese código." });

            p.Nombre = dto.Nombre;
            p.CodigoMakor = codigo;
            p.CategoriaId = dto.CategoriaId;
            p.SubcategoriaId = dto.SubcategoriaId;
            p.Descripcion = dto.Descripcion;
            p.PrecioMayorista = dto.PrecioBase;
            p.PrecioMinorista = dto.PrecioBase * 1.21m * 1.70m;
            p.Destacado = dto.Destacado;
            p.Activo = dto.Visible;

            if (!string.IsNullOrEmpty(dto.ImagenUrl))
            {
                var img = p.Imagenes.FirstOrDefault(i => i.EsPrincipal && !i.EsDeProveedor);
                if (img == null)
                {
                    p.Imagenes.Add(new Models.ProductoImagen { UrlOriginal = dto.ImagenUrl, EsPrincipal = true, EsDeProveedor = false });
                }
                else
                {
                    img.UrlOriginal = dto.ImagenUrl;
                }
            }
        }

        if (dto.Variantes != null)
        {
            var incomingIds = dto.Variantes.Where(v => v.Id.HasValue).Select(v => v.Id.Value).ToList();
            var toRemove = p.Variantes.Where(v => !incomingIds.Contains(v.Id)).ToList();
            _db.ProductoVariantes.RemoveRange(toRemove);

            foreach(var v in dto.Variantes)
            {
                if (v.Id.HasValue && v.Id.Value > 0)
                {
                    var existing = p.Variantes.FirstOrDefault(ev => ev.Id == v.Id.Value);
                    if (existing != null)
                    {
                        existing.ColorId = v.ColorId;
                        existing.Talle = v.Talle;
                        existing.Medida = v.Medida;
                        existing.CodigoArticulo = v.CodigoArticulo;
                        existing.Activo = v.Activo;
                        existing.Orden = v.Orden;
                    }
                }
                else
                {
                    p.Variantes.Add(new ProductoVariante {
                        ColorId = v.ColorId,
                        Talle = v.Talle,
                        Medida = v.Medida,
                        CodigoArticulo = v.CodigoArticulo,
                        Activo = v.Activo,
                        Orden = v.Orden
                    });
                }
            }
        }

        if (dto.RelacionadosIds != null)
        {
            _db.Set<ProductoRelacionado>().RemoveRange(p.Relacionados);
            foreach(var relId in dto.RelacionadosIds)
            {
                p.Relacionados.Add(new ProductoRelacionado { ProductoVinculadoId = relId });
            }
        }

        if (dto.TagIds != null)
        {
            _db.Set<ProductoTag>().RemoveRange(p.Tags);
            foreach (var tagId in dto.TagIds.Distinct())
            {
                p.Tags.Add(new ProductoTag { TagId = tagId });
            }
        }

        if (p.EsVentaLibre) p.Activo = false;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPost("{id}/sync")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<SyncResponse>> SyncProducto(int id)
    {
        var prod = await _db.Productos
            .Include(p => p.Proveedor)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (prod == null) return NotFound();

        if (prod.EsVentaLibre || prod.Proveedor?.Slug != "makor")
            return BadRequest(new { message = "Solo los productos de Makor se pueden sincronizar." });

        var result = await _sync.SyncProductoAsync(id);
        if (!result.Success)
            return BadRequest(new { message = "No se pudo sincronizar el producto con Makor." });

        return result;
    }

    // One-shot: limpia NombrePublico de productos Makor que quedaron cortados a mitad de
    // palabra por el bug viejo del parser de unidades (ej: "...CBX 20,5CM" -> "...CB").
    // Solo toca los que son prefijo exacto del nombre real cortando un token; no toca ediciones manuales.
    [HttpPost("mantenimiento/recalcular-titulos")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> RecalcularTitulosPublicos()
    {
        var makor = await _db.Productos
            .Where(p => p.ProveedorId == 1 && p.NombrePublico != null && p.NombrePublico != "")
            .ToListAsync();

        var afectados = new List<object>();

        foreach (var p in makor)
        {
            var raw = (p.Nombre ?? "").Trim();
            var stored = p.NombrePublico!.Trim();

            var cortadoAMitadDePalabra =
                stored.Length > 0 &&
                stored.Length < raw.Length &&
                raw.StartsWith(stored, StringComparison.Ordinal) &&
                !char.IsWhiteSpace(raw[stored.Length]);

            if (cortadoAMitadDePalabra)
            {
                afectados.Add(new { p.Id, p.Nombre, viejo = stored, nuevo = MakorPublicContent.ResolveTitle(p.Nombre!, null) });
                p.NombrePublico = null;
            }
        }

        if (afectados.Count > 0)
            await _db.SaveChangesAsync();

        return Ok(new { corregidos = afectados.Count, detalle = afectados });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Delete(int id)
    {
        var p = await _db.Productos.FindAsync(id);
        if (p == null) return NotFound();

        if (p.EsVentaLibre)
            return BadRequest(new { message = "El producto de venta libre no se puede eliminar." });

        try
        {
            await using var tx = await _db.Database.BeginTransactionAsync();

            await _db.ProductoRelacionados
                .Where(r => r.ProductoVinculadoId == id)
                .ExecuteDeleteAsync();

            _db.Productos.Remove(p);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
            return Ok();
        }
        catch (DbUpdateException ex)
        {
            return Conflict(new
            {
                message = "No se pudo eliminar el producto porque tiene datos vinculados que no se pudieron liberar.",
                detail = ex.InnerException?.Message ?? ex.Message
            });
        }
    }
}
