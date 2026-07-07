using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Helpers;
using MMatilde.Api.Models;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/turnos-venta")]
[Authorize]
public class TurnosVentaController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TurnosVentaService _turnos;

    public TurnosVentaController(AppDbContext db, TurnosVentaService turnos)
    {
        _db = db;
        _turnos = turnos;
    }

    [HttpGet]
    public async Task<ActionResult<List<TurnoVentaDto>>> GetAll()
    {
        await _turnos.EnsureSeedAsync();
        return await MapQuery(_db.TurnosVenta.OrderBy(t => t.Orden).ThenBy(t => t.HoraDesde));
    }

    [HttpGet("activos")]
    public async Task<ActionResult<List<TurnoVentaDto>>> GetActivos()
    {
        await _turnos.EnsureSeedAsync();
        return await MapQuery(_db.TurnosVenta.Where(t => t.Activo).OrderBy(t => t.HoraDesde).ThenBy(t => t.Orden));
    }

    [HttpPost]
    public async Task<ActionResult<TurnoVentaDto>> Create([FromBody] TurnoVentaCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { message = "El nombre es obligatorio." });

        var nombre = dto.Nombre.Trim();
        var slug = SlugHelper.Slugify(nombre).ToUpperInvariant().Replace('-', '_');
        if (string.IsNullOrEmpty(slug))
            return BadRequest(new { message = "Nombre inválido." });

        if (await _db.TurnosVenta.AnyAsync(t => t.Slug == slug))
            return BadRequest(new { message = "Ya existe un turno con ese nombre." });

        TimeOnly hora;
        try
        {
            hora = TurnosVentaService.ParseHora(dto.HoraDesde);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var turno = new TurnoVentaConfig
        {
            Nombre = nombre,
            Slug = slug,
            Orden = dto.Orden,
            Activo = dto.Activo,
            HoraDesde = hora,
        };

        _db.TurnosVenta.Add(turno);
        await _db.SaveChangesAsync();

        try
        {
            TurnosVentaService.Validate(await _db.TurnosVenta.ToListAsync());
        }
        catch (InvalidOperationException ex)
        {
            _db.TurnosVenta.Remove(turno);
            await _db.SaveChangesAsync();
            return BadRequest(new { message = ex.Message });
        }

        return (await MapQuery(_db.TurnosVenta.Where(t => t.Id == turno.Id))).First();
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TurnoVentaDto>> Update(int id, [FromBody] TurnoVentaUpdateDto dto)
    {
        var turno = await _db.TurnosVenta.FindAsync(id);
        if (turno == null) return NotFound();

        var nombre = dto.Nombre.Trim();
        var slug = SlugHelper.Slugify(nombre).ToUpperInvariant().Replace('-', '_');
        if (await _db.TurnosVenta.AnyAsync(t => t.Id != id && t.Slug == slug))
            return BadRequest(new { message = "Ya existe otro turno con ese nombre." });

        TimeOnly hora;
        try
        {
            hora = TurnosVentaService.ParseHora(dto.HoraDesde);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        var slugAnterior = turno.Slug;
        turno.Nombre = nombre;
        turno.Slug = slug;
        turno.Orden = dto.Orden;
        turno.Activo = dto.Activo;
        turno.HoraDesde = hora;
        turno.UpdatedAt = DateTime.UtcNow;

        if (!dto.Activo)
        {
            var otrosActivos = await _db.TurnosVenta.CountAsync(t => t.Activo && t.Id != id);
            if (otrosActivos < 2)
                return BadRequest(new { message = "Debe quedar al menos 2 turnos activos." });
        }

        if (slugAnterior != slug)
        {
            var ventas = await _db.Ventas.Where(v => v.Turno == slugAnterior).ToListAsync();
            foreach (var v in ventas)
                v.Turno = slug;
        }

        await _db.SaveChangesAsync();

        try
        {
            TurnosVentaService.Validate(await _db.TurnosVenta.ToListAsync());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return (await MapQuery(_db.TurnosVenta.Where(t => t.Id == id))).First();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (await _db.TurnosVenta.CountAsync() <= 2)
            return BadRequest(new { message = "Debe quedar al menos 2 turnos." });

        var turno = await _db.TurnosVenta.FindAsync(id);
        if (turno == null) return NotFound();

        var enUso = await _db.Ventas.AnyAsync(v => v.Turno == turno.Slug);
        if (enUso)
            return BadRequest(new { message = "No se puede eliminar: hay ventas con este turno. Desactivalo en su lugar." });

        _db.TurnosVenta.Remove(turno);
        await _db.SaveChangesAsync();

        try
        {
            TurnosVentaService.Validate(await _db.TurnosVenta.ToListAsync());
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        return NoContent();
    }

    private async Task<List<TurnoVentaDto>> MapQuery(IQueryable<TurnoVentaConfig> query)
    {
        var items = await query.ToListAsync();
        var activos = items.Where(t => t.Activo).OrderBy(t => t.HoraDesde).ThenBy(t => t.Orden).ToList();
        return items
            .OrderBy(t => t.Orden)
            .ThenBy(t => t.HoraDesde)
            .Select(t => TurnosVentaService.MapDto(t, activos))
            .ToList();
    }
}
