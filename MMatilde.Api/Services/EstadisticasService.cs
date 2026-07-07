using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Services;

public class EstadisticasService
{
    private readonly AppDbContext _db;

    public EstadisticasService(AppDbContext db) => _db = db;

    public async Task<EstadisticasResumenDto> GetResumenAsync(
        DateOnly desde,
        DateOnly hasta,
        TurnoVenta? turno,
        string? medioPagoSlug,
        bool comparar)
    {
        var (desdeUtc, _) = VentasService.RangoDiaArgentina(desde);
        var (_, hastaUtc) = VentasService.RangoDiaArgentina(hasta);

        var ventas = await QueryVentas(desdeUtc, hastaUtc, turno, medioPagoSlug).ToListAsync();
        var lineas = await QueryLineas(desdeUtc, hastaUtc, turno, medioPagoSlug).ToListAsync();

        var kpis = BuildKpis(ventas, lineas);
        EstadisticasKpiDto? anterior = null;

        if (comparar)
        {
            var dias = hasta.DayNumber - desde.DayNumber + 1;
            var prevHasta = desde.AddDays(-1);
            var prevDesde = prevHasta.AddDays(-(dias - 1));
            var (pDesdeUtc, _) = VentasService.RangoDiaArgentina(prevDesde);
            var (_, pHastaUtc) = VentasService.RangoDiaArgentina(prevHasta);
            var ventasPrev = await QueryVentas(pDesdeUtc, pHastaUtc, turno, medioPagoSlug).ToListAsync();
            var lineasPrev = await QueryLineas(pDesdeUtc, pHastaUtc, turno, medioPagoSlug).ToListAsync();
            anterior = BuildKpis(ventasPrev, lineasPrev);
        }

        var mediosMap = await _db.MediosPago.ToDictionaryAsync(m => m.Slug, m => m.Nombre);
        var categoriasMap = await _db.Categorias.ToDictionaryAsync(c => c.Id, c => c.Nombre);
        var productoCategorias = await _db.Productos
            .Where(p => lineas.Select(l => l.ProductoId).Distinct().Contains(p.Id))
            .Select(p => new { p.Id, p.CategoriaId })
            .ToDictionaryAsync(p => p.Id, p => p.CategoriaId);

        return new EstadisticasResumenDto(
            desde.ToString("yyyy-MM-dd"),
            hasta.ToString("yyyy-MM-dd"),
            kpis,
            anterior,
            BuildPorDia(ventas),
            BuildPorTurno(ventas),
            BuildTopProductos(lineas),
            BuildPorCategoria(lineas, productoCategorias, categoriasMap),
            BuildPorMedioPago(ventas, mediosMap),
            BuildPorOrigen(lineas)
        );
    }

    private IQueryable<Venta> QueryVentas(DateTime desdeUtc, DateTime hastaUtc, TurnoVenta? turno, string? medioPagoSlug)
    {
        var q = _db.Ventas.Where(v => v.Fecha >= desdeUtc && v.Fecha <= hastaUtc);
        if (turno.HasValue) q = q.Where(v => v.Turno == turno.Value);
        if (!string.IsNullOrWhiteSpace(medioPagoSlug))
            q = q.Where(v => v.MedioPagoSlug == medioPagoSlug.Trim().ToLowerInvariant());
        return q;
    }

    private IQueryable<VentaLinea> QueryLineas(DateTime desdeUtc, DateTime hastaUtc, TurnoVenta? turno, string? medioPagoSlug)
    {
        var q = _db.VentaLineas
            .Include(l => l.Venta)
            .Where(l => l.Venta != null && l.Venta.Fecha >= desdeUtc && l.Venta.Fecha <= hastaUtc);

        if (turno.HasValue) q = q.Where(l => l.Venta!.Turno == turno.Value);
        if (!string.IsNullOrWhiteSpace(medioPagoSlug))
            q = q.Where(l => l.Venta!.MedioPagoSlug == medioPagoSlug.Trim().ToLowerInvariant());

        return q;
    }

    private static EstadisticasKpiDto BuildKpis(List<Venta> ventas, List<VentaLinea> lineas)
    {
        var facturacion = ventas.Sum(v => v.Total);
        var ganancia = ventas.Sum(v => v.GananciaNetaEstimada);
        var count = ventas.Count;
        var items = lineas.Sum(l => l.Cantidad);

        return new EstadisticasKpiDto(
            facturacion,
            ganancia,
            facturacion > 0 ? Math.Round(ganancia / facturacion * 100m, 1) : 0m,
            count,
            count > 0 ? Math.Round(facturacion / count, 2) : 0m,
            items
        );
    }

    private static List<EstadisticasSerieDiaDto> BuildPorDia(List<Venta> ventas)
    {
        return ventas
            .GroupBy(v => VentasService.ToArgentina(new DateTimeOffset(v.Fecha, TimeSpan.Zero)).ToString("yyyy-MM-dd"))
            .OrderBy(g => g.Key)
            .Select(g => new EstadisticasSerieDiaDto(
                g.Key,
                g.Sum(v => v.Total),
                g.Sum(v => v.GananciaNetaEstimada),
                g.Count()
            ))
            .ToList();
    }

    private static List<EstadisticasSerieTurnoDto> BuildPorTurno(List<Venta> ventas) =>
        ventas
            .GroupBy(v => v.Turno.ToString())
            .Select(g => new EstadisticasSerieTurnoDto(
                g.Key,
                g.Sum(v => v.Total),
                g.Sum(v => v.GananciaNetaEstimada),
                g.Count()
            ))
            .OrderBy(x => x.Turno)
            .ToList();

    private static List<EstadisticasTopProductoDto> BuildTopProductos(List<VentaLinea> lineas) =>
        lineas
            .GroupBy(l => new { l.ProductoId, l.ProductoNombre })
            .Select(g => new EstadisticasTopProductoDto(
                g.Key.ProductoId,
                g.Key.ProductoNombre,
                g.Sum(l => l.Cantidad),
                g.Sum(l => l.Cantidad * l.PrecioUnitarioVenta),
                g.Sum(l => l.GananciaNetaEstimada)
            ))
            .OrderByDescending(x => x.Facturacion)
            .Take(15)
            .ToList();

    private static List<EstadisticasSerieCategoriaDto> BuildPorCategoria(
        List<VentaLinea> lineas,
        Dictionary<int, int> productoCategorias,
        Dictionary<int, string> categoriasMap)
    {
        return lineas
            .GroupBy(l =>
            {
                if (!productoCategorias.TryGetValue(l.ProductoId, out var catId))
                    return (Id: (int?)null, Nombre: "Sin categoría");
                var nombre = categoriasMap.GetValueOrDefault(catId, "Sin categoría");
                return (Id: (int?)catId, Nombre: nombre);
            })
            .Select(g => new EstadisticasSerieCategoriaDto(
                g.Key.Id,
                g.Key.Nombre,
                g.Sum(l => l.Cantidad * l.PrecioUnitarioVenta),
                g.Sum(l => l.GananciaNetaEstimada),
                g.Sum(l => l.Cantidad)
            ))
            .OrderByDescending(x => x.Facturacion)
            .Take(12)
            .ToList();
    }

    private static List<EstadisticasSerieMedioPagoDto> BuildPorMedioPago(
        List<Venta> ventas,
        Dictionary<string, string> mediosMap) =>
        ventas
            .GroupBy(v => v.MedioPagoSlug)
            .Select(g => new EstadisticasSerieMedioPagoDto(
                g.Key,
                mediosMap.GetValueOrDefault(g.Key, g.Key),
                g.Sum(v => v.Total),
                g.Count()
            ))
            .OrderByDescending(x => x.Facturacion)
            .ToList();

    private static List<EstadisticasSerieOrigenDto> BuildPorOrigen(List<VentaLinea> lineas) =>
        lineas
            .GroupBy(l => l.ModoOrigenEconomico.ToString())
            .Select(g => new EstadisticasSerieOrigenDto(
                g.Key,
                g.Sum(l => l.Cantidad * l.PrecioUnitarioVenta),
                g.Sum(l => l.GananciaNetaEstimada)
            ))
            .OrderByDescending(x => x.Facturacion)
            .ToList();
}
