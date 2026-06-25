using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;

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
            .Select(c => new CategoriaCardDto(c.Nombre, c.Icono ?? "", c.Slug))
            .ToListAsync();

        var prods = await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes)
            .Where(p => p.Activo)
            .OrderByDescending(p => p.Id)
            .Take(8)
            .Select(p => new ProductoCatalogoDto(
                p.Id,
                p.Slug,
                p.Nombre,
                p.Categoria != null ? p.Categoria.Nombre : "",
                p.Imagenes.OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden).Select(i => i.UrlOriginal).FirstOrDefault()
            ))
            .ToListAsync();

        return new HomeDataDto(cats, prods);
    }

    [HttpGet("buscar")]
    public async Task<ActionResult<List<ProductoCatalogoDto>>> Buscar([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 3) return new List<ProductoCatalogoDto>();

        var prods = await _db.Productos
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes)
            .Where(p => p.Activo && (EF.Functions.ILike(p.Nombre, $"%{q}%") || EF.Functions.ILike(p.CodigoMakor, $"%{q}%")))
            .OrderByDescending(p => p.Id)
            .Take(20)
            .Select(p => new ProductoCatalogoDto(
                p.Id,
                p.Slug,
                p.Nombre,
                p.Categoria != null ? p.Categoria.Nombre : "",
                p.Imagenes.OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden).Select(i => i.UrlOriginal).FirstOrDefault()
            ))
            .ToListAsync();

        return prods;
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
