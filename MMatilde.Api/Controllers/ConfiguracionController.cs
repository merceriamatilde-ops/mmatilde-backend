using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;

namespace MMatilde.Api.Controllers;

[Route("api/configuracion")]
[ApiController]
public class ConfiguracionController : ControllerBase
{
    private readonly AppDbContext _db;

    public ConfiguracionController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<Dictionary<string, string>>> Get()
    {
        var configs = await _db.ConfiguracionSitio.ToListAsync();
        var result = new Dictionary<string, string>();
        foreach (var c in configs)
        {
            result[c.Clave] = c.Valor;
        }
        return result;
    }

    [HttpPut]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update([FromBody] ConfiguracionUpdateRequest req)
    {
        var configs = await _db.ConfiguracionSitio.ToListAsync();
        foreach (var kvp in req.Values)
        {
            var config = configs.FirstOrDefault(c => c.Clave == kvp.Key);
            if (config != null)
            {
                config.Valor = kvp.Value;
                config.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _db.ConfiguracionSitio.Add(new Models.ConfiguracionSitio 
                { 
                    Clave = kvp.Key, 
                    Valor = kvp.Value,
                    Grupo = "General",
                    Label = kvp.Key
                });
            }
        }
        await _db.SaveChangesAsync();
        return Ok(new { success = true });
    }
}
