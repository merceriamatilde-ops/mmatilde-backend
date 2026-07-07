using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Services;

/// <summary>Estima la ganancia neta de la mercería según el origen económico del producto.</summary>
public static class GananciaService
{
    public static decimal CalcularMargenElaboracion(Producto producto)
    {
        var baseCosto = (producto.CostoMateriales ?? 0m) + (producto.ManoObra ?? 0m);
        if (producto.MargenElaboracionMonto.HasValue)
            return producto.MargenElaboracionMonto.Value;

        var pct = producto.MargenElaboracionPorcentaje ?? 0m;
        return Math.Round(baseCosto * pct / 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static decimal? CalcularPrecioElaboracion(Producto producto)
    {
        var baseCosto = (producto.CostoMateriales ?? 0m) + (producto.ManoObra ?? 0m);
        if (baseCosto <= 0 && !producto.MargenElaboracionMonto.HasValue && !producto.MargenElaboracionPorcentaje.HasValue)
            return null;

        return Math.Round(baseCosto + CalcularMargenElaboracion(producto), 2, MidpointRounding.AwayFromZero);
    }

    public static decimal CostoConIva(decimal costoSinIva, decimal ivaPorcentaje) =>
        Math.Round(costoSinIva * (1 + ivaPorcentaje / 100m), 2, MidpointRounding.AwayFromZero);

    public static GananciaEstimadaDto Estimar(
        Producto producto,
        decimal precioVenta,
        decimal? costoCompraPresentacion = null,
        decimal? ivaPorcentaje = null)
    {
        return producto.ModoOrigenEconomico switch
        {
            ModoOrigenEconomico.CONSIGNACION => EstimarConsignacion(producto, precioVenta),
            ModoOrigenEconomico.ELABORACION_PROPIA => EstimarElaboracion(producto, precioVenta),
            ModoOrigenEconomico.SIN_COSTO => new GananciaEstimadaDto(
                0m,
                precioVenta,
                100m,
                "Ganancia total: no hay costo de adquisición."
            ),
            _ => EstimarReventa(precioVenta, costoCompraPresentacion, ivaPorcentaje ?? 0m),
        };
    }

    private static GananciaEstimadaDto EstimarReventa(decimal precioVenta, decimal? costoCompra, decimal ivaPorcentaje)
    {
        if (!costoCompra.HasValue)
        {
            return new GananciaEstimadaDto(null, null, null, "Falta costo de compra para estimar ganancia.");
        }

        var costoConIva = CostoConIva(costoCompra.Value, ivaPorcentaje);
        var ganancia = precioVenta - costoConIva;
        var nota = ganancia < 0
            ? $"Pérdida estimada: venta por debajo de costo + IVA (${costoConIva:N2}). El IVA no se cuenta como ganancia."
            : "Reventa: precio de venta menos costo de compra con IVA. El IVA no es ganancia.";

        return new GananciaEstimadaDto(
            costoConIva,
            ganancia,
            precioVenta > 0 ? ganancia / precioVenta * 100m : 0m,
            nota
        );
    }

    private static GananciaEstimadaDto EstimarConsignacion(Producto producto, decimal precioVenta)
    {
        var pct = producto.ComisionTiendaPorcentaje ?? 0m;
        var ganancia = Math.Round(precioVenta * pct / 100m, 2, MidpointRounding.AwayFromZero);
        var aTitular = precioVenta - ganancia;
        return new GananciaEstimadaDto(
            aTitular,
            ganancia,
            pct,
            $"Consignación: la mercería retiene {pct}% (${ganancia:N2}); ${aTitular:N2} corresponde al titular."
        );
    }

    private static GananciaEstimadaDto EstimarElaboracion(Producto producto, decimal precioVenta)
    {
        var materiales = producto.CostoMateriales ?? 0m;
        var manoObra = producto.ManoObra ?? 0m;
        var baseCosto = materiales + manoObra;
        var margenObjetivo = CalcularMargenElaboracion(producto);
        var ganancia = precioVenta - baseCosto;

        var nota = ganancia < 0
            ? $"Pérdida estimada: venta por debajo del costo (materiales + mano de obra = ${baseCosto:N2}). La ganancia objetivo era el margen (${margenObjetivo:N2})."
            : $"Elaboración: ganancia = precio − (materiales + mano de obra). Margen objetivo: ${margenObjetivo:N2}. Materiales y mano de obra no son ganancia.";

        return new GananciaEstimadaDto(
            baseCosto,
            ganancia,
            precioVenta > 0 ? ganancia / precioVenta * 100m : null,
            nota
        );
    }
}
