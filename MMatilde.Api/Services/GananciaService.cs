using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Services;

/// <summary>Estima la ganancia neta de la mercería según el origen económico del producto.</summary>
public static class GananciaService
{
    public static GananciaEstimadaDto Estimar(Producto producto, decimal precioVenta, decimal? costoCompraPresentacion = null)
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
            _ => EstimarReventa(precioVenta, costoCompraPresentacion),
        };
    }

    private static GananciaEstimadaDto EstimarReventa(decimal precioVenta, decimal? costoCompra)
    {
        if (!costoCompra.HasValue)
        {
            return new GananciaEstimadaDto(null, null, null, "Falta costo de compra para estimar ganancia.");
        }

        var ganancia = precioVenta - costoCompra.Value;
        return new GananciaEstimadaDto(
            costoCompra.Value,
            ganancia,
            precioVenta > 0 ? ganancia / precioVenta * 100m : 0m,
            "Reventa: precio de venta menos costo de compra."
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
        var costoTotal = materiales + manoObra;
        var ganancia = manoObra;
        var nota = manoObra > 0
            ? $"Elaboración: materiales ${materiales:N2} + mano de obra ${manoObra:N2}. Ganancia mercería ≈ mano de obra."
            : $"Elaboración: materiales ${materiales:N2}. Definí mano de obra para ver ganancia.";

        if (precioVenta > 0 && costoTotal > 0 && Math.Abs(precioVenta - costoTotal) > 0.01m)
            nota += $" Precio venta (${precioVenta:N2}) difiere de materiales+MO (${costoTotal:N2}).";

        return new GananciaEstimadaDto(
            materiales,
            ganancia,
            precioVenta > 0 ? ganancia / precioVenta * 100m : null,
            nota
        );
    }
}
