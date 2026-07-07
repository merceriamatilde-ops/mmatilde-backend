using MMatilde.Api.Models;

namespace MMatilde.Api.DTOs;

public record VentaLineaCreateDto(
    int ProductoId,
    decimal Cantidad,
    decimal? PrecioUnitario
);

public record VentaCreateDto(
    DateTimeOffset FechaHora,
    TurnoVenta Turno,
    string MedioPagoSlug,
    string? Notas,
    List<VentaLineaCreateDto> Lineas
);

public record VentaUpdateDto(
    DateTimeOffset FechaHora,
    TurnoVenta Turno,
    string MedioPagoSlug,
    string? Notas,
    List<VentaLineaCreateDto> Lineas
);

public record VentaLineaDto(
    int Id,
    int ProductoId,
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

public record ProductoVentaBusquedaDto(
    int Id,
    string Nombre,
    string? CodigoMakor,
    decimal? PrecioVenta,
    string? UnidadVenta,
    decimal? GananciaNetaEstimada,
    string ModoOrigenEconomico
);

public record ProductoVentaPrecioDto(
    int Id,
    string Nombre,
    decimal? PrecioVenta,
    string? UnidadVenta,
    decimal? GananciaNetaEstimada,
    string ModoOrigenEconomico,
    string? NotaGanancia
);
