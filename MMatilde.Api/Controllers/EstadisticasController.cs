using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/estadisticas")]
[Authorize]
public class EstadisticasController : ControllerBase
{
    private readonly EstadisticasService _stats;

    public EstadisticasController(EstadisticasService stats) => _stats = stats;

    [HttpGet("resumen")]
    public async Task<ActionResult<EstadisticasResumenDto>> Resumen(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] TurnoVenta? turno,
        [FromQuery] string? medioPago,
        [FromQuery] bool comparar = true)
    {
        if (hasta < desde)
            return BadRequest(new { message = "La fecha hasta debe ser posterior a desde." });

        return await _stats.GetResumenAsync(desde, hasta, turno, medioPago, comparar);
    }
}
