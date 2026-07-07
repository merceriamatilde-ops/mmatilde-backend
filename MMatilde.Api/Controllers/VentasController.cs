using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/ventas")]
[Authorize]
public class VentasController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly VentasService _ventas;

    public VentasController(AppDbContext db, VentasService ventas)
    {
        _db = db;
        _ventas = ventas;
    }

    [HttpGet("productos-buscar")]
    public async Task<ActionResult<List<ProductoVentaBusquedaDto>>> BuscarProductos([FromQuery] string q, [FromQuery] int limit = 8)
    {
        return await _ventas.BuscarProductosAsync(q, limit);
    }

    [HttpGet("producto/{id}/precio")]
    public async Task<ActionResult<ProductoVentaPrecioDto>> GetProductoPrecio(int id)
    {
        var dto = await _ventas.GetProductoPrecioAsync(id);
        if (dto == null) return NotFound();
        return dto;
    }

    [HttpGet("turno-sugerido")]
    public async Task<ActionResult<object>> GetTurnoSugerido([FromQuery] DateTimeOffset? fechaHora = null)
    {
        var when = fechaHora ?? DateTimeOffset.UtcNow;
        var local = VentasService.ToArgentina(when);
        return new
        {
            turno = await _ventas.InferirTurnoAsync(when),
            fechaHoraLocal = local.ToString("yyyy-MM-ddTHH:mm"),
        };
    }

    [HttpGet]
    public async Task<ActionResult<List<VentaListDto>>> List(
        [FromQuery] DateOnly? desde,
        [FromQuery] DateOnly? hasta,
        [FromQuery] string? turno,
        [FromQuery] string? q,
        [FromQuery] string? ordenar = "fecha",
        [FromQuery] string? direccion = "desc",
        [FromQuery] int limit = 100)
    {
        var query = _db.Ventas
            .Include(v => v.Lineas)
            .AsQueryable();

        if (desde.HasValue)
        {
            var (desdeUtc, _) = VentasService.RangoDiaArgentina(desde.Value);
            query = query.Where(v => v.Fecha >= desdeUtc);
        }

        if (hasta.HasValue)
        {
            var (_, hastaUtc) = VentasService.RangoDiaArgentina(hasta.Value);
            query = query.Where(v => v.Fecha <= hastaUtc);
        }

        if (!string.IsNullOrWhiteSpace(turno))
            query = query.Where(v => v.Turno == turno.Trim().ToUpperInvariant());

        if (!string.IsNullOrWhiteSpace(q))
        {
            var patron = $"%{q.Trim()}%";
            query = query.Where(v =>
                v.Lineas.Any(l => EF.Functions.ILike(l.ProductoNombre, patron)) ||
                (v.Notas != null && EF.Functions.ILike(v.Notas, patron)));
        }

        var asc = string.Equals(direccion, "asc", StringComparison.OrdinalIgnoreCase);
        query = (ordenar?.ToLowerInvariant()) switch
        {
            "total" => asc ? query.OrderBy(v => v.Total) : query.OrderByDescending(v => v.Total),
            "ganancia" => asc ? query.OrderBy(v => v.GananciaNetaEstimada) : query.OrderByDescending(v => v.GananciaNetaEstimada),
            "items" => asc ? query.OrderBy(v => v.Lineas.Count) : query.OrderByDescending(v => v.Lineas.Count),
            "turno" => asc ? query.OrderBy(v => v.Turno).ThenByDescending(v => v.Fecha)
                : query.OrderByDescending(v => v.Turno).ThenByDescending(v => v.Fecha),
            "medio" => asc ? query.OrderBy(v => v.MedioPagoSlug).ThenByDescending(v => v.Fecha)
                : query.OrderByDescending(v => v.MedioPagoSlug).ThenByDescending(v => v.Fecha),
            _ => asc ? query.OrderBy(v => v.Fecha).ThenBy(v => v.Id)
                : query.OrderByDescending(v => v.Fecha).ThenByDescending(v => v.Id),
        };

        var ventas = await query
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync();

        var mediosMap = await _ventas.GetMediosNombreMapAsync();
        return ventas.Select(v => _ventas.MapList(v, mediosMap)).ToList();
    }

    [HttpGet("resumen")]
    public async Task<ActionResult<VentaResumenDto>> Resumen(
        [FromQuery] DateOnly fecha,
        [FromQuery] string turno)
    {
        var turnoSlug = turno.Trim().ToUpperInvariant();
        var (desdeUtc, hastaUtc) = VentasService.RangoDiaArgentina(fecha);

        var ventas = await _db.Ventas
            .Where(v => v.Fecha >= desdeUtc && v.Fecha <= hastaUtc && v.Turno == turnoSlug)
            .ToListAsync();

        var count = ventas.Count;
        var total = ventas.Sum(v => v.Total);
        var ganancia = ventas.Sum(v => v.GananciaNetaEstimada);

        return new VentaResumenDto(
            fecha.ToString("yyyy-MM-dd"),
            turnoSlug,
            count,
            total,
            ganancia,
            count > 0 ? Math.Round(total / count, 2, MidpointRounding.AwayFromZero) : 0m
        );
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<VentaDetailDto>> Get(int id)
    {
        var venta = await _db.Ventas
            .Include(v => v.Lineas)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (venta == null) return NotFound();
        var mediosMap = await _ventas.GetMediosNombreMapAsync();
        return _ventas.MapDetail(venta, mediosMap);
    }

    [HttpPost]
    public async Task<ActionResult<VentaDetailDto>> Create([FromBody] VentaCreateDto dto)
    {
        try
        {
            var venta = await _ventas.CrearVentaAsync(dto);
            var mediosMap = await _ventas.GetMediosNombreMapAsync();
            return CreatedAtAction(nameof(Get), new { id = venta.Id }, _ventas.MapDetail(venta, mediosMap));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<VentaDetailDto>> Update(int id, [FromBody] VentaUpdateDto dto)
    {
        try
        {
            var venta = await _ventas.ActualizarVentaAsync(id, dto);
            if (venta == null) return NotFound();
            var mediosMap = await _ventas.GetMediosNombreMapAsync();
            return _ventas.MapDetail(venta, mediosMap);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var venta = await _db.Ventas.FindAsync(id);
        if (venta == null) return NotFound();
        _db.Ventas.Remove(venta);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

