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

    private async Task<int> GetConfigIntAsync(string clave, int fallback)
    {
        var cfg = await _db.ConfiguracionSitio.FirstOrDefaultAsync(c => c.Clave == clave);
        return cfg != null && int.TryParse(cfg.Valor, out var v) && v > 0 ? v : fallback;
    }

    [HttpGet("home")]
    public async Task<ActionResult<HomeDataDto>> GetHomeData()
    {
        // La cantidad visible se controla por config (mobile/desktop). Traemos el máximo de ambos
        // y el front recorta por breakpoint.
        var maxDesktop = await GetConfigIntAsync("home_max_categorias_desktop", 6);
        var maxMobile = await GetConfigIntAsync("home_max_categorias_mobile", 4);
        var take = Math.Clamp(Math.Max(maxDesktop, maxMobile), 1, 24);

        var cats = await _db.Categorias
            .Where(c => c.Activo && c.Productos.Any(p => p.Activo))
            .OrderBy(c => c.Orden)
            .Take(take)
            .Select(c => new CategoriaCardDto(c.Nombre, c.Icono ?? "", c.Slug, c.Productos.Count(p => p.Activo), c.Imagen))
            .ToListAsync();

        // "Te puede interesar": más vendidos primero; si faltan, se rellena con productos random.
        // Siempre activos y sin venta libre.
        var takeProds = Math.Clamp(await GetConfigIntAsync("home_max_destacados", 12), 1, 60);

        var masVendidosIds = await _db.VentaLineas
            .Where(l => l.ProductoId != null && l.Producto != null && l.Producto.Activo && !l.Producto.EsVentaLibre)
            .GroupBy(l => l.ProductoId!.Value)
            .Select(g => new { Id = g.Key, Total = g.Sum(x => x.Cantidad) })
            .OrderByDescending(x => x.Total)
            .Take(takeProds)
            .Select(x => x.Id)
            .ToListAsync();

        var faltan = takeProds - masVendidosIds.Count;
        var randomIds = faltan > 0
            ? await _db.Productos
                .Where(p => p.Activo && !p.EsVentaLibre && !masVendidosIds.Contains(p.Id))
                .OrderBy(p => EF.Functions.Random())
                .Take(faltan)
                .Select(p => p.Id)
                .ToListAsync()
            : new List<int>();

        var ordenIds = masVendidosIds.Concat(randomIds).ToList();

        var prodEntities = await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes)
            .Where(p => ordenIds.Contains(p.Id))
            .ToListAsync();

        var prods = ordenIds
            .Select(id => prodEntities.FirstOrDefault(p => p.Id == id))
            .Where(p => p != null)
            .Select(p => ProductoDisplay.ToCatalogoDto(p!))
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

        return new HomeDataDto(cats, prods, colecciones, maxMobile, maxDesktop);
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
                .Where(p => p.Activo && !p.EsVentaLibre && (
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

        var productosBase = _db.Productos
            .Where(p => p.Activo && p.Tags.Any(pt => pt.TagId == tag.Id));

        var categorias = (await productosBase
            .Where(p => p.Categoria != null)
            .Select(p => new { p.CategoriaId, Nombre = p.Categoria!.Nombre, Slug = p.Categoria!.Slug })
            .ToListAsync())
            .GroupBy(p => new { p.CategoriaId, p.Nombre, p.Slug })
            .Select(g => new ColeccionCategoriaFiltroDto(
                g.Key.CategoriaId,
                g.Key.Nombre,
                g.Key.Slug,
                g.Count()))
            .OrderBy(c => c.Nombre)
            .ToList();

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
