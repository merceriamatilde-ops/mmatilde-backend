using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Services;

public class TurnosVentaService
{
    private readonly AppDbContext _db;

    public TurnosVentaService(AppDbContext db) => _db = db;

    public async Task<List<TurnoVentaConfig>> GetActivosAsync() =>
        await _db.TurnosVenta
            .Where(t => t.Activo)
            .OrderBy(t => t.HoraDesde)
            .ThenBy(t => t.Orden)
            .ToListAsync();

    public async Task<string> InferirSlugAsync(DateTimeOffset fechaHora)
    {
        await EnsureSeedAsync();
        var turnos = await GetActivosAsync();
        var local = VentasService.ToArgentina(fechaHora);
        return InferirSlug(TimeOnly.FromDateTime(local.DateTime), turnos);
    }

    public static string InferirSlug(TimeOnly time, IReadOnlyList<TurnoVentaConfig> turnos)
    {
        var activos = turnos.Where(t => t.Activo).OrderBy(t => t.HoraDesde).ThenBy(t => t.Orden).ToList();
        if (activos.Count == 0) return "MANANA";

        var pick = activos[0];
        foreach (var t in activos)
        {
            if (time >= t.HoraDesde) pick = t;
        }

        return pick.Slug;
    }

    public static void Validate(IReadOnlyList<TurnoVentaConfig> turnos)
    {
        if (turnos.Count < 2)
            throw new InvalidOperationException("Debe haber al menos 2 turnos configurados.");

        var activos = turnos.Where(t => t.Activo).OrderBy(t => t.HoraDesde).ThenBy(t => t.Orden).ToList();
        if (activos.Count < 2)
            throw new InvalidOperationException("Debe haber al menos 2 turnos activos.");

        var horas = activos.Select(t => t.HoraDesde).ToList();
        if (horas.Distinct().Count() != horas.Count)
            throw new InvalidOperationException("Los horarios de inicio no pueden repetirse entre turnos.");

        if (activos[0].HoraDesde != TimeOnly.MinValue)
            throw new InvalidOperationException("El primer turno debe comenzar a las 00:00.");
    }

    public static string DescribirHorario(TurnoVentaConfig turno, IReadOnlyList<TurnoVentaConfig> sortedActivos)
    {
        var idx = sortedActivos.ToList().FindIndex(t => t.Id == turno.Id);
        if (idx < 0) return string.Empty;

        var next = idx < sortedActivos.Count - 1 ? sortedActivos[idx + 1] : null;
        if (next != null)
            return $"De {turno.HoraDesde:HH\\:mm} a antes de {next.HoraDesde:HH\\:mm}";

        return $"Desde las {turno.HoraDesde:HH\\:mm} hasta fin del día";
    }

    public static TurnoVentaDto MapDto(TurnoVentaConfig t, IReadOnlyList<TurnoVentaConfig> sortedActivos) =>
        new(
            t.Id,
            t.Slug,
            t.Nombre,
            t.Orden,
            t.Activo,
            t.HoraDesde.ToString("HH:mm"),
            DescribirHorario(t, sortedActivos)
        );

    public static TimeOnly ParseHora(string value)
    {
        if (!TimeOnly.TryParse(value, out var hora))
            throw new InvalidOperationException("Hora inválida. Usá formato HH:mm.");
        return hora;
    }

    public async Task EnsureSeedAsync()
    {
        if (await _db.TurnosVenta.AnyAsync()) return;

        _db.TurnosVenta.AddRange(
            new TurnoVentaConfig
            {
                Slug = "MANANA",
                Nombre = "Mañana",
                Orden = 1,
                HoraDesde = new TimeOnly(0, 0),
            },
            new TurnoVentaConfig
            {
                Slug = "TARDE",
                Nombre = "Tarde",
                Orden = 2,
                HoraDesde = new TimeOnly(14, 0),
            }
        );
        await _db.SaveChangesAsync();
    }
}
