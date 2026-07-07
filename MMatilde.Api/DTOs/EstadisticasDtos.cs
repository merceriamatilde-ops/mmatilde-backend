namespace MMatilde.Api.DTOs;

public record EstadisticasKpiDto(
    decimal Facturacion,
    decimal GananciaNeta,
    decimal MargenPorcentaje,
    int CantidadVentas,
    decimal TicketPromedio,
    decimal ItemsVendidos
);

public record EstadisticasSerieDiaDto(string Fecha, decimal Facturacion, decimal Ganancia, int Ventas);

public record EstadisticasSerieTurnoDto(string Turno, decimal Facturacion, decimal Ganancia, int Ventas);

public record EstadisticasTopProductoDto(
    int ProductoId,
    string ProductoNombre,
    decimal Cantidad,
    decimal Facturacion,
    decimal Ganancia
);

public record EstadisticasSerieCategoriaDto(
    int? CategoriaId,
    string CategoriaNombre,
    decimal Facturacion,
    decimal Ganancia,
    decimal Cantidad
);

public record EstadisticasSerieMedioPagoDto(string MedioPagoSlug, string MedioPagoNombre, decimal Facturacion, int Ventas);

public record EstadisticasSerieOrigenDto(string OrigenEconomico, decimal Facturacion, decimal Ganancia);

public record EstadisticasResumenDto(
    string Desde,
    string Hasta,
    EstadisticasKpiDto Kpis,
    EstadisticasKpiDto? KpisPeriodoAnterior,
    List<EstadisticasSerieDiaDto> PorDia,
    List<EstadisticasSerieTurnoDto> PorTurno,
    List<EstadisticasTopProductoDto> TopProductos,
    List<EstadisticasSerieCategoriaDto> PorCategoria,
    List<EstadisticasSerieMedioPagoDto> PorMedioPago,
    List<EstadisticasSerieOrigenDto> PorOrigenEconomico
);
