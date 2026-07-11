using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Services;

namespace MMatilde.Api.Controllers;

[Route("api/sync")]
[ApiController]
[Authorize(Roles = "ADMIN")]
public class SyncController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SyncService _syncService;

    public SyncController(AppDbContext db, SyncService syncService)
    {
        _db = db;
        _syncService = syncService;
    }

    [HttpPost]
    public async Task<ActionResult<SyncResponse>> ExecuteSync([FromBody] SyncRequest req)
    {
        if (req.Terms == null || req.Terms.Count == 0) return BadRequest("No terms provided");
        
        var result = await _syncService.ExecuteSync(req.Terms);
        return result;
    }

    [HttpGet("logs")]
    public async Task<ActionResult<List<SyncLogDto>>> GetLogs()
    {
        var logs = await _db.SyncLogs
            .OrderByDescending(l => l.IniciadoEn)
            .Take(10)
            .Select(l => new SyncLogDto(
                l.Id,
                l.Estado.ToString(),
                l.ProductosNuevos,
                l.ProductosActualizados,
                l.Errores,
                l.TermsJson,
                l.CategoriasJson,
                l.ResumenJson,
                l.DetalleErrores,
                l.IniciadoEn,
                l.FinalizadoEn
            ))
            .ToListAsync();

        return logs;
    }
}
