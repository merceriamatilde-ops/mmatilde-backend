using Microsoft.EntityFrameworkCore;
using MMatilde.Api.Data;
using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Services;

public class VentasService
{
    private readonly AppDbContext _db;
    private readonly PricingService _pricing;
    private readonly TurnosVentaService _turnos;

    public VentasService(AppDbContext db, PricingService pricing, TurnosVentaService turnos)
    {
        _db = db;
        _pricing = pricing;
        _turnos = turnos;
    }

    public async Task<string> InferirTurnoAsync(DateTimeOffset fechaHora) =>
        await _turnos.InferirSlugAsync(fechaHora);

    public static DateTimeOffset ToArgentina(DateTimeOffset value)
    {
        var tz = ArgentinaTimeZone();
        return TimeZoneInfo.ConvertTime(value, tz);
    }

    public static (DateTime DesdeUtc, DateTime HastaUtc) RangoDiaArgentina(DateOnly fecha)
    {
        var tz = ArgentinaTimeZone();
        var inicioLocal = new DateTime(fecha.Year, fecha.Month, fecha.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var finLocal = inicioLocal.AddDays(1).AddTicks(-1);
        return (
            TimeZoneInfo.ConvertTimeToUtc(inicioLocal, tz),
            TimeZoneInfo.ConvertTimeToUtc(finLocal, tz)
        );
    }

    private static TimeZoneInfo ArgentinaTimeZone() =>
        TimeZoneInfo.FindSystemTimeZoneById(
            OperatingSystem.IsWindows() ? "Argentina Standard Time" : "America/Argentina/Buenos_Aires");

    public async Task<ProductoVentaPrecioDto?> GetProductoPrecioAsync(int productoId)
    {
        var producto = await LoadProducto(productoId);
        if (producto == null) return null;
        return await MapProductoPrecio(producto);
    }

    public async Task<List<ProductoVentaBusquedaDto>> BuscarProductosAsync(string q, int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
            return [];

        var patron = $"%{q.Trim()}%";
        var productos = await _db.Productos
            .Include(p => p.Presentaciones)
            .Include(p => p.Variantes)
                .ThenInclude(v => v.Color)
            .Where(p => p.Activo && (
                EF.Functions.ILike(p.Nombre, patron) ||
                (p.NombrePublico != null && EF.Functions.ILike(p.NombrePublico, patron)) ||
                EF.Functions.ILike(p.CodigoMakor, patron)))
            .OrderBy(p => p.Nombre)
            .Take(Math.Clamp(limit, 5, 30))
            .ToListAsync();

        var persistPresentaciones = false;
        foreach (var p in productos)
        {
            if (await _pricing.EnsurePresentacionVentaListaAsync(p))
                persistPresentaciones = true;
        }
        if (persistPresentaciones)
            await _db.SaveChangesAsync();

        var result = new List<ProductoVentaBusquedaDto>();
        foreach (var p in productos)
        {
            var presentaciones = await MapPresentacionesVenta(p);
            var defaultPres = ResolvePresentacion(p, null);
            var mapped = await MapPresentacionPrecio(p, defaultPres);
            result.Add(new ProductoVentaBusquedaDto(
                p.Id,
                p.NombrePublico ?? p.Nombre,
                p.CodigoMakor,
                mapped.Precio,
                mapped.UnidadVenta,
                mapped.GananciaNetaEstimada,
                p.ModoOrigenEconomico.ToString(),
                mapped.CostoReferencia,
                mapped.IvaPorcentaje,
                mapped.CostoMateriales,
                mapped.ManoObra,
                MapVariantesVenta(p),
                presentaciones
            ));
        }

        return result;
    }

    public async Task<Dictionary<string, string>> GetMediosNombreMapAsync() =>
        await _db.MediosPago.ToDictionaryAsync(m => m.Slug, m => m.Nombre);

    public async Task<Venta> CrearVentaAsync(VentaCreateDto dto)
    {
        if (dto.Lineas.Count == 0)
            throw new InvalidOperationException("La venta debe tener al menos una línea.");

        var medioSlug = await ValidarMedioPagoAsync(dto.MedioPagoSlug);
        var turnoSlug = await ValidarTurnoAsync(dto.Turno);

        var venta = new Venta
        {
            Fecha = dto.FechaHora.UtcDateTime,
            Turno = turnoSlug,
            MedioPagoSlug = medioSlug,
            Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim(),
        };

        foreach (var linea in dto.Lineas)
            venta.Lineas.Add(await BuildLineaAsync(linea));

        venta.Total = venta.Lineas.Sum(l => l.Cantidad * l.PrecioUnitarioVenta);
        venta.GananciaNetaEstimada = venta.Lineas.Sum(l => l.GananciaNetaEstimada);

        _db.Ventas.Add(venta);
        await _db.SaveChangesAsync();
        return venta;
    }

    public async Task<Venta?> ActualizarVentaAsync(int id, VentaUpdateDto dto)
    {
        var venta = await _db.Ventas
            .Include(v => v.Lineas)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (venta == null) return null;

        if (dto.Lineas.Count == 0)
            throw new InvalidOperationException("La venta debe tener al menos una línea.");

        venta.Fecha = dto.FechaHora.UtcDateTime;
        venta.Turno = await ValidarTurnoAsync(dto.Turno);
        venta.MedioPagoSlug = await ValidarMedioPagoAsync(dto.MedioPagoSlug);
        venta.Notas = string.IsNullOrWhiteSpace(dto.Notas) ? null : dto.Notas.Trim();

        _db.VentaLineas.RemoveRange(venta.Lineas);
        venta.Lineas.Clear();

        foreach (var linea in dto.Lineas)
            venta.Lineas.Add(await BuildLineaAsync(linea));

        venta.Total = venta.Lineas.Sum(l => l.Cantidad * l.PrecioUnitarioVenta);
        venta.GananciaNetaEstimada = venta.Lineas.Sum(l => l.GananciaNetaEstimada);

        await _db.SaveChangesAsync();
        return venta;
    }

    public VentaListDto MapList(Venta venta, IReadOnlyDictionary<string, string> mediosMap) => new(
        venta.Id,
        ToArgentina(new DateTimeOffset(venta.Fecha, TimeSpan.Zero)),
        venta.Turno,
        venta.MedioPagoSlug,
        mediosMap.GetValueOrDefault(venta.MedioPagoSlug, venta.MedioPagoSlug),
        venta.Total,
        venta.GananciaNetaEstimada,
        venta.Lineas.Count,
        venta.Notas
    );

    public VentaDetailDto MapDetail(Venta venta, IReadOnlyDictionary<string, string> mediosMap) => new(
        venta.Id,
        ToArgentina(new DateTimeOffset(venta.Fecha, TimeSpan.Zero)),
        venta.Turno,
        venta.MedioPagoSlug,
        mediosMap.GetValueOrDefault(venta.MedioPagoSlug, venta.MedioPagoSlug),
        venta.Total,
        venta.GananciaNetaEstimada,
        venta.Notas,
        venta.Lineas
            .OrderBy(l => l.Id)
            .Select(l => new VentaLineaDto(
                l.Id,
                l.ProductoId,
                l.VarianteId,
                l.VarianteLabel,
                l.PresentacionId,
                l.PresentacionNombre,
                l.ProductoNombre,
                l.Cantidad,
                l.PrecioUnitarioVenta,
                Math.Round(l.Cantidad * l.PrecioUnitarioVenta, 2, MidpointRounding.AwayFromZero),
                l.ModoOrigenEconomico.ToString(),
                l.GananciaNetaEstimada
            ))
            .ToList()
    );

    private async Task<Producto?> LoadProducto(int id)
    {
        var producto = await _db.Productos
            .Include(p => p.Presentaciones)
            .Include(p => p.Variantes)
                .ThenInclude(v => v.Color)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null) return null;

        if (await _pricing.EnsurePresentacionVentaListaAsync(producto))
            await _db.SaveChangesAsync();

        return producto;
    }

    private static List<ProductoVentaVarianteDto> MapVariantesVenta(Producto producto) =>
        producto.Variantes
            .Where(v => v.Activo)
            .OrderBy(v => v.Orden)
            .ThenBy(v => v.Id)
            .Select(v => new ProductoVentaVarianteDto(v.Id, BuildVarianteLabel(v)))
            .ToList();

    private static string BuildVarianteLabel(ProductoVariante variante)
    {
        var partes = new List<string>();
        if (!string.IsNullOrWhiteSpace(variante.Color?.Nombre))
            partes.Add(variante.Color.Nombre.Trim());
        if (!string.IsNullOrWhiteSpace(variante.Talle))
            partes.Add(variante.Talle.Trim());
        if (!string.IsNullOrWhiteSpace(variante.Medida))
            partes.Add(variante.Medida.Trim());
        if (partes.Count > 0)
            return string.Join(" · ", partes);
        if (!string.IsNullOrWhiteSpace(variante.CodigoArticulo))
            return variante.CodigoArticulo.Trim();
        return $"Variante #{variante.Id}";
    }

    private async Task<VentaLinea> BuildLineaAsync(VentaLineaCreateDto input)
    {
        var producto = await LoadProducto(input.ProductoId)
            ?? throw new InvalidOperationException($"Producto {input.ProductoId} no encontrado.");

        if (input.Cantidad <= 0)
            throw new InvalidOperationException("La cantidad debe ser mayor a cero.");

        ProductoVariante? variante = null;
        if (input.VarianteId.HasValue)
        {
            variante = producto.Variantes.FirstOrDefault(v => v.Id == input.VarianteId.Value && v.Activo)
                ?? throw new InvalidOperationException("La variante seleccionada no es válida.");
        }

        var pres = ResolvePresentacion(producto, input.PresentacionId);
        var mapped = await MapPresentacionPrecio(producto, pres);

        var precioUnitario = input.PrecioUnitario ?? mapped.Precio
            ?? throw new InvalidOperationException($"El producto \"{producto.Nombre}\" no tiene precio de venta.");

        var est = GananciaService.Estimar(producto, precioUnitario, mapped.CostoCompra, mapped.IvaPorcentaje);
        var gananciaUnit = est.GananciaNetaEstimada ?? 0m;

        return new VentaLinea
        {
            ProductoId = producto.Id,
            VarianteId = variante?.Id,
            VarianteLabel = variante != null ? BuildVarianteLabel(variante) : null,
            PresentacionId = pres?.Id,
            PresentacionNombre = pres?.Nombre,
            ProductoNombre = producto.NombrePublico ?? producto.Nombre,
            Cantidad = input.Cantidad,
            PrecioUnitarioVenta = precioUnitario,
            ModoOrigenEconomico = producto.ModoOrigenEconomico,
            CostoCompraSnapshot = mapped.CostoCompra,
            CostoMaterialesSnapshot = producto.CostoMateriales,
            ManoObraSnapshot = producto.ManoObra,
            ComisionTiendaPorcentajeSnapshot = producto.ComisionTiendaPorcentaje,
            GananciaNetaEstimada = Math.Round(gananciaUnit * input.Cantidad, 2, MidpointRounding.AwayFromZero),
        };
    }

    private sealed record PresentacionPrecioMapped(
        decimal? Precio,
        string? UnidadVenta,
        decimal? GananciaNetaEstimada,
        decimal? CostoReferencia,
        decimal? CostoCompra,
        decimal? IvaPorcentaje,
        decimal? CostoMateriales,
        decimal? ManoObra
    );

    private static ProductoPresentacion? ResolvePresentacion(Producto producto, int? presentacionId)
    {
        if (presentacionId.HasValue)
        {
            var selected = producto.Presentaciones.FirstOrDefault(p => p.Id == presentacionId.Value && p.Activo);
            if (selected != null) return selected;
        }

        return producto.Presentaciones.FirstOrDefault(p => p.EsDefault && p.Activo)
            ?? producto.Presentaciones.FirstOrDefault(p => p.Activo);
    }

    private async Task<List<ProductoVentaPresentacionDto>> MapPresentacionesVenta(Producto producto)
    {
        var list = new List<ProductoVentaPresentacionDto>();
        foreach (var pres in producto.Presentaciones.Where(p => p.Activo).OrderBy(p => p.Orden).ThenBy(p => p.Id))
        {
            var mapped = await MapPresentacionPrecio(producto, pres);
            list.Add(new ProductoVentaPresentacionDto(
                pres.Id,
                pres.Nombre,
                mapped.Precio,
                mapped.GananciaNetaEstimada,
                mapped.CostoReferencia,
                pres.EsDefault
            ));
        }

        return list;
    }

    private async Task<PresentacionPrecioMapped> MapPresentacionPrecio(Producto producto, ProductoPresentacion? pres)
    {
        decimal? precio = pres?.PrecioVenta;
        if (pres != null && !precio.HasValue)
            precio = await _pricing.CalcularPrecioVentaAsync(producto, pres);
        precio ??= producto.PrecioMinorista;

        var tienePresentacionesActivas = producto.Presentaciones.Any(p => p.Activo);
        var unidadVenta = pres?.Nombre;
        if (string.IsNullOrWhiteSpace(unidadVenta) && !tienePresentacionesActivas)
            unidadVenta = producto.EtiquetaUnidadCompra;

        var costoBase = _pricing.CostoPorUnidadBase(producto);
        decimal? costoCompra = null;
        if (costoBase.HasValue && pres != null)
            costoCompra = costoBase.Value * pres.CantidadUnidadBase;

        var iva = await _pricing.ResolveIvaAsync(producto);
        GananciaEstimadaDto? ganancia = null;
        if (precio.HasValue)
            ganancia = GananciaService.Estimar(producto, precio.Value, costoCompra, iva);

        return new PresentacionPrecioMapped(
            precio,
            unidadVenta,
            ganancia?.GananciaNetaEstimada,
            ganancia?.CostoReferencia,
            costoCompra,
            iva,
            producto.CostoMateriales,
            producto.ManoObra
        );
    }

    private async Task<ProductoVentaPrecioDto> MapProductoPrecio(Producto producto)
    {
        var pres = ResolvePresentacion(producto, null);
        var mapped = await MapPresentacionPrecio(producto, pres);
        var presentaciones = await MapPresentacionesVenta(producto);

        GananciaEstimadaDto? ganancia = null;
        if (mapped.Precio.HasValue)
            ganancia = GananciaService.Estimar(producto, mapped.Precio.Value, mapped.CostoCompra, mapped.IvaPorcentaje);

        return new ProductoVentaPrecioDto(
            producto.Id,
            producto.NombrePublico ?? producto.Nombre,
            mapped.Precio,
            mapped.UnidadVenta,
            ganancia?.GananciaNetaEstimada,
            producto.ModoOrigenEconomico.ToString(),
            ganancia?.Nota,
            ganancia?.CostoReferencia,
            mapped.IvaPorcentaje,
            mapped.CostoMateriales,
            mapped.ManoObra,
            presentaciones
        );
    }

    private async Task<string> ValidarMedioPagoAsync(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("El medio de pago es obligatorio.");

        var normalizado = slug.Trim().ToLowerInvariant();
        var medio = await _db.MediosPago.FirstOrDefaultAsync(m => m.Slug == normalizado && m.Activo);
        if (medio == null)
            throw new InvalidOperationException("Medio de pago inválido o inactivo.");

        return medio.Slug;
    }

    private async Task<string> ValidarTurnoAsync(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            throw new InvalidOperationException("El turno es obligatorio.");

        var normalizado = slug.Trim().ToUpperInvariant();
        var turno = await _db.TurnosVenta.FirstOrDefaultAsync(t => t.Slug == normalizado && t.Activo);
        if (turno == null)
            throw new InvalidOperationException("Turno inválido o inactivo.");

        return turno.Slug;
    }
}
