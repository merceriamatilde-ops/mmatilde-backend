using MMatilde.Api.Models;

namespace MMatilde.Api.DTOs;

public record VentaLineaCreateDto(
    int ProductoId,
    int? VarianteId,
    int? PresentacionId,
    decimal Cantidad,
    decimal? PrecioUnitario
);

public record VentaCreateDto(
    DateTimeOffset FechaHora,
    string Turno,
    string MedioPagoSlug,
    string? Notas,
    List<VentaLineaCreateDto> Lineas
);

public record VentaUpdateDto(
    DateTimeOffset FechaHora,
    string Turno,
    string MedioPagoSlug,
    string? Notas,
    List<VentaLineaCreateDto> Lineas
);

public record VentaLineaDto(
    int Id,
    int ProductoId,
    int? VarianteId,
    string? VarianteLabel,
    int? PresentacionId,
    string? PresentacionNombre,
    string ProductoNombre,
    decimal Cantidad,
    decimal PrecioUnitarioVenta,
    decimal Subtotal,
    string ModoOrigenEconomico,
    decimal GananciaNetaEstimada
);

public record VentaListDto(
    int Id,
    DateTimeOffset FechaHora,
    string Turno,
    string MedioPagoSlug,
    string MedioPagoNombre,
    decimal Total,
    decimal GananciaNetaEstimada,
    int CantidadLineas,
    string? Notas
);

public record VentaDetailDto(
    int Id,
    DateTimeOffset FechaHora,
    string Turno,
    string MedioPagoSlug,
    string MedioPagoNombre,
    decimal Total,
    decimal GananciaNetaEstimada,
    string? Notas,
    List<VentaLineaDto> Lineas
);

public record VentaResumenDto(
    string Fecha,
    string Turno,
    int CantidadVentas,
    decimal TotalFacturado,
    decimal GananciaNetaEstimada,
    decimal TicketPromedio
);

public record ProductoVentaVarianteDto(
    int Id,
    string Label
);

public record ProductoVentaPresentacionDto(
    int Id,
    string Nombre,
    decimal? PrecioVenta,
    decimal? GananciaNetaEstimada,
    decimal? CostoReferencia,
    bool EsDefault
);

public record ProductoVentaBusquedaDto(
    int Id,
    string Nombre,
    string? CodigoMakor,
    decimal? PrecioVenta,
    string? UnidadVenta,
    decimal? GananciaNetaEstimada,
    string ModoOrigenEconomico,
    decimal? CostoReferencia,
    decimal? IvaPorcentaje,
    decimal? CostoMateriales,
    decimal? ManoObra,
    List<ProductoVentaVarianteDto> Variantes,
    List<ProductoVentaPresentacionDto> Presentaciones
);

public record ProductoVentaPrecioDto(
    int Id,
    string Nombre,
    decimal? PrecioVenta,
    string? UnidadVenta,
    decimal? GananciaNetaEstimada,
    string ModoOrigenEconomico,
    string? NotaGanancia,
    decimal? CostoReferencia,
    decimal? IvaPorcentaje,
    decimal? CostoMateriales,
    decimal? ManoObra,
    List<ProductoVentaPresentacionDto> Presentaciones
);
