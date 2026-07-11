using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;

namespace MMatilde.Api.Services;

public class UsuarioFiltroService
{
    private readonly AppDbContext _db;

    public UsuarioFiltroService(AppDbContext db) => _db = db;

    public async Task<List<UsuarioFiltroDto>> ListarParaFiltroVentasAsync()
    {
        var activos = await _db.Usuarios
            .Where(u => u.EliminadoEn == null)
            .OrderBy(u => u.Nombre)
            .Select(u => new UsuarioFiltroDto(u.Id, u.Nombre, false))
            .ToListAsync();

        var idsActivos = activos.Select(u => u.Id).ToHashSet();

        var archivados = await _db.Usuarios
            .Where(u => u.EliminadoEn != null && !idsActivos.Contains(u.Id))
            .Where(u => _db.Ventas.Any(v => v.UsuarioId == u.Id))
            .OrderBy(u => u.Nombre)
            .Select(u => new UsuarioFiltroDto(u.Id, u.Nombre, true))
            .ToListAsync();

        return activos.Concat(archivados).ToList();
    }
}
