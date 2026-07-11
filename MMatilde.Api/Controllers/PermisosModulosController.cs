using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MMatilde.Api.DTOs;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[Route("api/permisos-modulos")]
[ApiController]
[Authorize]
public class PermisosModulosController : ControllerBase
{
    private readonly PermisosModulosService _permisos;

    public PermisosModulosController(PermisosModulosService permisos) => _permisos = permisos;

    [HttpGet]
    public async Task<ActionResult<PermisosModulosDto>> Get() => await _permisos.GetAsync();

    [HttpGet("definiciones")]
    public ActionResult<List<ModuloDefDto>> Definiciones() =>
        PermisosModulosService.Definiciones.ToList();

    [HttpPut]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<PermisosModulosDto>> Update([FromBody] PermisosModulosUpdateDto dto) =>
        await _permisos.SaveAsync(dto);
}
