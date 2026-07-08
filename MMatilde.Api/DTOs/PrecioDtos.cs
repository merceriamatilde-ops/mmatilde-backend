using MMatilde.Api.Models;

namespace MMatilde.Api.DTOs;

public record PrecioConfigDto(decimal IvaPorcentaje, decimal MargenGlobal);

public record ReglaPrecioDto(
    int Id,
    int? CategoriaId,
    string? CategoriaNombre,
    int? SubcategoriaId,
    string? SubcategoriaNombre,
    int? MarcaId,
    decimal MargenPorcentaje,
    string Tipo,
    bool Activo
);

public record ReglaPrecioCreateDto(
    int? CategoriaId,
    int? SubcategoriaId,
    int? MarcaId,
    decimal MargenPorcentaje,
    TipoPrecio Tipo
);

public record PresentacionDto(
    int? Id,
    string Nombre,
    decimal CantidadUnidadBase,
    decimal? PrecioVenta,
    decimal? MargenPorcentaje,
    decimal? PrecioCalculado,
    bool EsDefault,
    bool Activo,
    int Orden
);

public record GananciaEstimadaDto(
    decimal? CostoReferencia,
    decimal? GananciaNetaEstimada,
    decimal? MargenSobreVentaPorcentaje,
    string Nota
);

public record PrecioVentaResumenDto(
    decimal? PrecioVentaFinal,
    decimal? PrecioVentaPorUnidad,
    decimal CantidadReferencia,
    string? PresentacionNombre
);

public record ProductoUnidadesDto(
    string? UnidadBase,
    decimal? CantidadUnidadCompra,
    string? EtiquetaUnidadCompra,
    bool UnidadCompraAutoDetectada,
    decimal? PrecioCompra,
    decimal? CostoPorUnidadBase,
    decimal IvaPorcentaje,
    decimal MargenAplicado,
    string ModoPrecio,
    decimal? IvaPorcentajeProducto,
    decimal? MargenPorcentajeProducto,
    decimal IvaGlobal,
    decimal MargenGlobal,
    string ModoOrigenEconomico,
    decimal? ComisionTiendaPorcentaje,
    string? TitularConsignacion,
    decimal? CostoMateriales,
    decimal? ManoObra,
    decimal? MargenElaboracionPorcentaje,
    decimal? MargenElaboracionMonto,
    GananciaEstimadaDto? GananciaEstimada,
    decimal? PrecioVentaFinal,
    decimal? PrecioVentaPorUnidad,
    decimal CantidadReferenciaVenta,
    string? PrecioVentaPresentacion,
    List<PresentacionDto> Presentaciones
);

public record ProductoUnidadesUpdateDto(
    string? UnidadBase,
    decimal? CantidadUnidadCompra,
    string? EtiquetaUnidadCompra,
    bool? UnidadCompraAutoDetectada,
    string? ModoPrecio,
    decimal? IvaPorcentajeProducto,
    decimal? MargenPorcentajeProducto,
    string? ModoOrigenEconomico,
    decimal? ComisionTiendaPorcentaje,
    string? TitularConsignacion,
    decimal? CostoMateriales,
    decimal? ManoObra,
    decimal? MargenElaboracionPorcentaje,
    decimal? MargenElaboracionMonto,
    List<PresentacionInputDto>? Presentaciones,
    decimal? PrecioCompra,
    bool RecalcularPrecios = true
);

public record PresentacionInputDto(
    int? Id,
    string Nombre,
    decimal CantidadUnidadBase,
    decimal? PrecioVenta,
    decimal? MargenPorcentaje,
    bool EsDefault,
    bool Activo,
    int Orden
);

public record UnidadSugeridaDto(
    string UnidadBase,
    decimal CantidadUnidadCompra,
    string Etiqueta,
    bool Confiable
);
