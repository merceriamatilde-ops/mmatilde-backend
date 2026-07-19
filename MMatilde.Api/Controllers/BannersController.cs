using MMatilde.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Controllers;

[Route("api/banners")]
[ApiController]
public class BannersController : ControllerBase
{
    private readonly AppDbContext _db;

    public BannersController(AppDbContext db)
    {
        _db = db;
    }

    private static (string? href, bool externo) ResolverLink(Banner b)
    {
        switch (b.LinkTipo)
        {
            case BannerLinkTipo.Categoria:
                return b.LinkCategoria != null ? ($"/categorias/{b.LinkCategoria.Slug}", false) : (null, false);
            case BannerLinkTipo.Coleccion:
                return b.LinkTag != null ? ($"/colecciones/{b.LinkTag.Slug}", false) : (null, false);
            case BannerLinkTipo.Url:
                if (string.IsNullOrWhiteSpace(b.LinkUrl)) return (null, false);
                var url = b.LinkUrl.Trim();
                var externo = url.StartsWith("http://") || url.StartsWith("https://");
                return (url, externo);
            default:
                return (null, false);
        }
    }

    private static bool EsVigente(Banner b, DateTime now) =>
        (b.FechaDesde == null || b.FechaDesde <= now) &&
        (b.FechaHasta == null || b.FechaHasta >= now);

    // Público: banners activos y vigentes de una ubicación, ordenados.
    [HttpGet("~/api/catalogo/banners")]
    public async Task<ActionResult<List<BannerPublicoDto>>> GetPublicos([FromQuery] string ubicacion = "home")
    {
        var now = DateTime.UtcNow;
        var banners = await _db.Banners
            .Include(b => b.LinkCategoria)
            .Include(b => b.LinkTag)
            .Where(b => b.Activo && b.Ubicacion == ubicacion)
            .Where(b => (b.FechaDesde == null || b.FechaDesde <= now) &&
                        (b.FechaHasta == null || b.FechaHasta >= now))
            .OrderBy(b => b.Orden)
            .ThenBy(b => b.Id)
            .ToListAsync();

        return banners.Select(b =>
        {
            var (href, externo) = ResolverLink(b);
            return new BannerPublicoDto(
                b.Id,
                b.ImagenDesktopUrl,
                b.ImagenMobileUrl,
                href,
                externo,
                b.AbreEnNuevaPestana,
                b.Titulo);
        }).ToList();
    }

    [HttpGet]
    [Authorize]
    [AuthorizeModule("banners")]
    public async Task<ActionResult<List<BannerAdminDto>>> GetAdmin([FromQuery] string? ubicacion)
    {
        var now = DateTime.UtcNow;
        var query = _db.Banners
            .Include(b => b.LinkCategoria)
            .Include(b => b.LinkTag)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(ubicacion))
            query = query.Where(b => b.Ubicacion == ubicacion);

        var banners = await query
            .OrderBy(b => b.Ubicacion)
            .ThenBy(b => b.Orden)
            .ThenBy(b => b.Id)
            .ToListAsync();

        return banners.Select(b =>
        {
            var (href, _) = ResolverLink(b);
            return new BannerAdminDto(
                b.Id,
                b.Titulo,
                b.ImagenDesktopUrl,
                b.ImagenMobileUrl,
                b.LinkTipo.ToString(),
                b.LinkCategoriaId,
                b.LinkTagId,
                b.LinkUrl,
                href,
                b.Ubicacion,
                b.Orden,
                b.Activo,
                b.AbreEnNuevaPestana,
                b.FechaDesde,
                b.FechaHasta,
                EsVigente(b, now));
        }).ToList();
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Create([FromBody] BannerUpsertDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Titulo))
            return BadRequest(new { message = "El título es obligatorio." });
        if (string.IsNullOrWhiteSpace(dto.ImagenDesktopUrl))
            return BadRequest(new { message = "La imagen es obligatoria." });

        var ubicacion = string.IsNullOrWhiteSpace(dto.Ubicacion) ? "home" : dto.Ubicacion.Trim();
        var maxOrden = await _db.Banners.Where(b => b.Ubicacion == ubicacion).MaxAsync(b => (int?)b.Orden) ?? -1;

        var banner = new Banner
        {
            Titulo = dto.Titulo.Trim(),
            ImagenDesktopUrl = dto.ImagenDesktopUrl.Trim(),
            Ubicacion = ubicacion,
            Orden = maxOrden + 1,
        };
        ApplyUpsert(banner, dto);

        _db.Banners.Add(banner);
        await _db.SaveChangesAsync();
        return Ok(new { id = banner.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Update(int id, [FromBody] BannerUpsertDto dto)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            return BadRequest(new { message = "El título es obligatorio." });
        if (string.IsNullOrWhiteSpace(dto.ImagenDesktopUrl))
            return BadRequest(new { message = "La imagen es obligatoria." });

        banner.Titulo = dto.Titulo.Trim();
        banner.ImagenDesktopUrl = dto.ImagenDesktopUrl.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Ubicacion))
            banner.Ubicacion = dto.Ubicacion.Trim();
        ApplyUpsert(banner, dto);
        banner.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return Ok();
    }

    private void ApplyUpsert(Banner banner, BannerUpsertDto dto)
    {
        banner.ImagenMobileUrl = string.IsNullOrWhiteSpace(dto.ImagenMobileUrl) ? null : dto.ImagenMobileUrl.Trim();
        banner.LinkTipo = Enum.TryParse<BannerLinkTipo>(dto.LinkTipo, true, out var tipo) ? tipo : BannerLinkTipo.Ninguno;
        banner.Activo = dto.Activo;
        banner.AbreEnNuevaPestana = dto.AbreEnNuevaPestana;
        banner.FechaDesde = dto.FechaDesde;
        banner.FechaHasta = dto.FechaHasta;

        // Solo persistimos el destino correspondiente al tipo elegido; el resto se limpia.
        banner.LinkCategoriaId = tipo == BannerLinkTipo.Categoria ? dto.LinkCategoriaId : null;
        banner.LinkTagId = tipo == BannerLinkTipo.Coleccion ? dto.LinkTagId : null;
        banner.LinkUrl = tipo == BannerLinkTipo.Url && !string.IsNullOrWhiteSpace(dto.LinkUrl) ? dto.LinkUrl.Trim() : null;
    }

    [HttpPut("orden")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Reorder([FromBody] BannerReorderDto dto)
    {
        if (dto.Ids == null || dto.Ids.Count == 0)
            return BadRequest(new { message = "No se recibió ningún orden." });

        var banners = await _db.Banners.Where(b => dto.Ids.Contains(b.Id)).ToListAsync();
        for (int i = 0; i < dto.Ids.Count; i++)
        {
            var banner = banners.FirstOrDefault(b => b.Id == dto.Ids[i]);
            if (banner != null) banner.Orden = i;
        }

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("{id}/toggle")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Toggle(int id, [FromBody] ToggleRequest req)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();

        banner.Activo = req.Value;
        banner.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult> Delete(int id)
    {
        var banner = await _db.Banners.FindAsync(id);
        if (banner == null) return NotFound();

        _db.Banners.Remove(banner);
        await _db.SaveChangesAsync();
        return Ok();
    }
}
