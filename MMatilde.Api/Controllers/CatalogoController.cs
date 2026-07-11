using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Helpers;

namespace MMatilde.Api.Controllers;

[Route("api/catalogo")]
[ApiController]
public class CatalogoController : ControllerBase
{
    private readonly AppDbContext _db;

    public CatalogoController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet("home")]
    public async Task<ActionResult<HomeDataDto>> GetHomeData()
    {
        var cats = await _db.Categorias
            .Where(c => c.Activo && c.Productos.Any(p => p.Activo))
            .OrderBy(c => c.Orden)
            .Take(8)
            .Select(c => new CategoriaCardDto(c.Nombre, c.Icono ?? "", c.Slug, c.Productos.Count(p => p.Activo)))
            .ToListAsync();

        var prods = (await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes)
            .Where(p => p.Activo && !p.EsVentaLibre)
            .OrderByDescending(p => p.Id)
            .Take(8)
            .ToListAsync())
            .Select(p => ProductoDisplay.ToCatalogoDto(p))
            .ToList();

        var colecciones = await _db.Tags
            .Where(t => t.Activo && t.VisibleEnCatalogo)
            .Where(t => t.Productos.Any(pt => pt.Producto.Activo))
            .OrderBy(t => t.Orden)
            .ThenBy(t => t.Nombre)
            .Take(6)
            .Select(t => new ColeccionCardDto(
                t.Nombre,
                t.Slug,
                t.Descripcion,
                t.ColorHex,
                t.Productos.Count(pt => pt.Producto.Activo)
            ))
            .ToListAsync();

        return new HomeDataDto(cats, prods, colecciones);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<ProductoCatalogoDto>>> Buscar([FromQuery] string q, [FromQuery] int limit = 30)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 3) return new List<ProductoCatalogoDto>();

        var tokens = CatalogSearchHelper.ExpandSearchTokens(q);
        if (tokens.Count == 0) return new List<ProductoCatalogoDto>();

        var take = Math.Clamp(limit, 5, 50);
        var vistos = new HashSet<int>();
        var resultados = new List<ProductoCatalogoDto>();

        foreach (var token in tokens)
        {
            if (resultados.Count >= take) break;

            var patron = $"%{token}%";
            var chunk = (await _db.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Subcategoria)
                .Include(p => p.Imagenes)
                .Where(p => p.Activo && (
                    EF.Functions.ILike(p.Nombre, patron) ||
                    (p.NombrePublico != null && EF.Functions.ILike(p.NombrePublico, patron)) ||
                    EF.Functions.ILike(p.CodigoMakor, patron) ||
                    (p.Composicion != null && EF.Functions.ILike(p.Composicion, patron)) ||
                    (p.Categoria != null && EF.Functions.ILike(p.Categoria.Nombre, patron)) ||
                    (p.Subcategoria != null && EF.Functions.ILike(p.Subcategoria.Nombre, patron))))
                .OrderByDescending(p => p.Destacado)
                .ThenByDescending(p => p.Id)
                .Take(take)
                .ToListAsync())
                .Select(p => ProductoDisplay.ToCatalogoDto(p))
                .ToList();

            foreach (var prod in chunk)
            {
                if (vistos.Add(prod.Id))
                    resultados.Add(prod);
                if (resultados.Count >= take) break;
            }
        }

        return resultados;
    }

    [HttpGet("colecciones")]
    public async Task<ActionResult<List<ColeccionCardDto>>> GetColecciones()
    {
        return await _db.Tags
            .Where(t => t.Activo && t.VisibleEnCatalogo)
            .Where(t => t.Productos.Any(pt => pt.Producto.Activo))
            .OrderBy(t => t.Orden)
            .ThenBy(t => t.Nombre)
            .Select(t => new ColeccionCardDto(
                t.Nombre,
                t.Slug,
                t.Descripcion,
                t.ColorHex,
                t.Productos.Count(pt => pt.Producto.Activo)
            ))
            .ToListAsync();
    }

    [HttpGet("colecciones/{slug}")]
    public async Task<ActionResult<ColeccionDetalleDto>> GetColeccion(string slug, [FromQuery] string? categoria)
    {
        var tag = await _db.Tags
            .Where(t => t.Slug == slug && t.Activo && t.VisibleEnCatalogo)
            .FirstOrDefaultAsync();

        if (tag == null) return NotFound();

        var productosBase = _db.ProductoTags
            .Where(pt => pt.TagId == tag.Id && pt.Producto.Activo)
            .Select(pt => pt.Producto);

        var categorias = await productosBase
            .Where(p => p.Categoria != null)
            .GroupBy(p => new { p.CategoriaId, Nombre = p.Categoria!.Nombre, Slug = p.Categoria!.Slug })
            .Select(g => new ColeccionCategoriaFiltroDto(
                g.Key.CategoriaId,
                g.Key.Nombre,
                g.Key.Slug,
                g.Count()))
            .OrderBy(c => c.Nombre)
            .ToListAsync();

        var productosQuery = productosBase;
        if (!string.IsNullOrWhiteSpace(categoria))
            productosQuery = productosQuery.Where(p => p.Categoria != null && p.Categoria.Slug == categoria);

        var productos = (await productosQuery
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes)
            .OrderByDescending(p => p.Id)
            .ToListAsync())
            .Select(p => ProductoDisplay.ToCatalogoDto(p))
            .ToList();

        return new ColeccionDetalleDto(tag.Nombre, tag.Slug, tag.Descripcion, tag.ColorHex, categorias, productos);
    }

    [HttpGet("dashboard")]
    [Authorize]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats()
    {
        var total = await _db.Productos.CountAsync();
        var activos = await _db.Productos.CountAsync(p => p.Activo);
        var cats = await _db.Categorias.CountAsync();

        return new DashboardStatsDto(total, activos, cats);
    }
}
