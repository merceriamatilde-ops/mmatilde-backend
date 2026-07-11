using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Helpers;
using MMatilde.Api.Models;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/medios-pago")]
[Authorize]
public class MediosPagoController : ControllerBase
{
    private readonly AppDbContext _db;

    public MediosPagoController(AppDbContext db) => _db = db;

    [HttpGet]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<List<MedioPagoDto>>> GetAll()
    {
        return await MapQuery(_db.MediosPago.OrderBy(m => m.Orden).ThenBy(m => m.Nombre));
    }

    [HttpGet("activos")]
    [AllowAnonymous]
    public async Task<ActionResult<List<MedioPagoDto>>> GetActivos()
    {
        await EnsureMediosSeedAsync();
        return await MapQuery(_db.MediosPago.Where(m => m.Activo).OrderBy(m => m.Orden).ThenBy(m => m.Nombre));
    }

    [HttpPost]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<MedioPagoDto>> Create([FromBody] MedioPagoCreateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Nombre))
            return BadRequest(new { message = "El nombre es obligatorio." });

        var nombre = dto.Nombre.Trim();
        var slug = SlugHelper.Slugify(nombre);
        if (string.IsNullOrEmpty(slug))
            return BadRequest(new { message = "Nombre inválido." });

        if (await _db.MediosPago.AnyAsync(m => m.Slug == slug))
            return BadRequest(new { message = "Ya existe un medio de pago con ese nombre." });

        var esPrimerMedio = !await _db.MediosPago.AnyAsync();
        var medio = new MedioPago
        {
            Nombre = nombre,
            Slug = slug,
            Orden = dto.Orden,
            Activo = dto.Activo,
            EsDefault = esPrimerMedio,
        };

        _db.MediosPago.Add(medio);
        await _db.SaveChangesAsync();

        return (await MapQuery(_db.MediosPago.Where(m => m.Id == medio.Id))).First();
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<MedioPagoDto>> Update(int id, [FromBody] MedioPagoUpdateDto dto)
    {
        var medio = await _db.MediosPago.FindAsync(id);
        if (medio == null) return NotFound();

        var nombre = dto.Nombre.Trim();
        var slug = SlugHelper.Slugify(nombre);
        if (await _db.MediosPago.AnyAsync(m => m.Id != id && m.Slug == slug))
            return BadRequest(new { message = "Ya existe otro medio de pago con ese nombre." });

        if (!dto.Activo && medio.Activo)
        {
            var otrosActivos = await _db.MediosPago.CountAsync(m => m.Activo && m.Id != id);
            if (otrosActivos == 0)
                return BadRequest(new { message = "Debe quedar al menos un medio de pago activo." });
        }

        var slugAnterior = medio.Slug;
        medio.Nombre = nombre;
        medio.Slug = slug;
        medio.Orden = dto.Orden;
        medio.Activo = dto.Activo;
        medio.UpdatedAt = DateTime.UtcNow;

        if (slugAnterior != slug)
        {
            var ventas = await _db.Ventas.Where(v => v.MedioPagoSlug == slugAnterior).ToListAsync();
            foreach (var v in ventas)
                v.MedioPagoSlug = slug;
        }

        if (!medio.Activo && medio.EsDefault)
        {
            medio.EsDefault = false;
            await AsignarDefaultSiFaltaAsync();
        }

        await _db.SaveChangesAsync();
        return (await MapQuery(_db.MediosPago.Where(m => m.Id == id))).First();
    }

    [HttpPut("{id}/default")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var medio = await _db.MediosPago.FindAsync(id);
        if (medio == null) return NotFound();
        if (!medio.Activo)
            return BadRequest(new { message = "Solo un medio activo puede ser el predeterminado." });

        var todos = await _db.MediosPago.ToListAsync();
        foreach (var m in todos)
            m.EsDefault = m.Id == id;

        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int id)
    {
        var medio = await _db.MediosPago.FindAsync(id);
        if (medio == null) return NotFound();

        var enUso = await _db.Ventas.AnyAsync(v => v.MedioPagoSlug == medio.Slug);
        if (enUso)
            return BadRequest(new { message = "No se puede eliminar: hay ventas registradas con este medio. Desactivalo en su lugar." });

        if (await _db.MediosPago.CountAsync() <= 1)
            return BadRequest(new { message = "Debe quedar al menos un medio de pago." });

        var eraDefault = medio.EsDefault;
        _db.MediosPago.Remove(medio);
        await _db.SaveChangesAsync();

        if (eraDefault)
            await AsignarDefaultSiFaltaAsync();

        return NoContent();
    }

    private async Task AsignarDefaultSiFaltaAsync()
    {
        if (await _db.MediosPago.AnyAsync(m => m.EsDefault && m.Activo)) return;
        var siguiente = await _db.MediosPago.Where(m => m.Activo).OrderBy(m => m.Orden).FirstOrDefaultAsync();
        if (siguiente != null)
        {
            siguiente.EsDefault = true;
            await _db.SaveChangesAsync();
        }
    }

    private async Task<List<MedioPagoDto>> MapQuery(IQueryable<MedioPago> query) =>
        await query.Select(m => new MedioPagoDto(m.Id, m.Nombre, m.Slug, m.Activo, m.EsDefault, m.Orden)).ToListAsync();

    private async Task EnsureMediosSeedAsync()
    {
        if (await _db.MediosPago.AnyAsync()) return;

        _db.MediosPago.AddRange(
            new MedioPago { Nombre = "Efectivo", Slug = "efectivo", EsDefault = true, Orden = 1 },
            new MedioPago { Nombre = "Transferencia", Slug = "transferencia", Orden = 2 },
            new MedioPago { Nombre = "Mixto", Slug = "mixto", Orden = 3 }
        );
        await _db.SaveChangesAsync();
    }
}
