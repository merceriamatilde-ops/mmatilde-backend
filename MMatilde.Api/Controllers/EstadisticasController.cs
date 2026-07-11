using MMatilde.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMatilde.Api.DTOs;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[ApiController]
[Route("api/estadisticas")]
[Authorize]
[AuthorizeModule("estadisticas")]
public class EstadisticasController : ControllerBase
{
    private readonly EstadisticasService _stats;
    private readonly UsuarioFiltroService _usuariosFiltro;

    public EstadisticasController(EstadisticasService stats, UsuarioFiltroService usuariosFiltro)
    {
        _stats = stats;
        _usuariosFiltro = usuariosFiltro;
    }

    [HttpGet("usuarios-filtro")]
    public async Task<ActionResult<List<UsuarioFiltroDto>>> UsuariosFiltro() =>
        await _usuariosFiltro.ListarParaFiltroVentasAsync();

    [HttpGet("resumen")]
    public async Task<ActionResult<EstadisticasResumenDto>> Resumen(
        [FromQuery] DateOnly desde,
        [FromQuery] DateOnly hasta,
        [FromQuery] string? turno,
        [FromQuery] string? medioPago,
        [FromQuery] bool comparar = true,
        [FromQuery] Guid? usuarioId = null,
        [FromQuery] bool sinUsuario = false)
    {
        if (hasta < desde)
            return BadRequest(new { message = "La fecha hasta debe ser posterior a desde." });

        return await _stats.GetResumenAsync(desde, hasta, turno, medioPago, comparar, usuarioId, sinUsuario);
    }
}
