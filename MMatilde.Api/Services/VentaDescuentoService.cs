namespace MMatilde.Api.Services;

public record VentaLineaDescuentoInput(
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal GananciaBrutaLinea,
    decimal DescuentoPorcentaje = 0,
    decimal DescuentoMonto = 0
);

public record VentaLineaDescuentoResult(
    decimal SubtotalBruto,
    decimal DescuentoLineaMonto,
    decimal SubtotalNeto,
    decimal DescuentoGlobalAsignado,
    decimal SubtotalFinal,
    decimal GananciaNetaEstimada
);

public record VentaDescuentoResult(
    decimal SubtotalBruto,
    decimal SubtotalNetoLineas,
    decimal DescuentoGlobalMonto,
    decimal Total,
    decimal GananciaNetaEstimada,
    IReadOnlyList<VentaLineaDescuentoResult> Lineas
);

/// <summary>
/// Descuentos de línea y global con prorrateo proporcional de ganancia.
/// </summary>
public static class VentaDescuentoService
{
    public static VentaDescuentoResult Calcular(
        IReadOnlyList<VentaLineaDescuentoInput> lineas,
        decimal descuentoGlobalPorcentaje,
        decimal? descuentoGlobalMontoInput = null)
    {
        if (lineas.Count == 0)
            return new VentaDescuentoResult(0, 0, 0, 0, 0, Array.Empty<VentaLineaDescuentoResult>());

        var intermediates = lineas.Select(l =>
        {
            var subBruto = Round(l.Cantidad * l.PrecioUnitario);
            var descLinea = l.DescuentoMonto > 0
                ? Round(l.DescuentoMonto)
                : Round(subBruto * ClampPct(l.DescuentoPorcentaje) / 100m);
            if (descLinea > subBruto) descLinea = subBruto;

            var subNeto = subBruto - descLinea;
            var ganAfterLine = l.GananciaBrutaLinea - descLinea;
            return new { subBruto, descLinea, subNeto, ganAfterLine };
        }).ToList();

        var baseGlobal = intermediates.Sum(x => x.subNeto);
        decimal descGlobal = descuentoGlobalMontoInput is > 0
            ? Round(descuentoGlobalMontoInput.Value)
            : Round(baseGlobal * ClampPct(descuentoGlobalPorcentaje) / 100m);
        if (descGlobal > baseGlobal) descGlobal = baseGlobal;

        var results = new List<VentaLineaDescuentoResult>();
        decimal assigned = 0;
        var maxIdx = 0;
        var maxNeto = 0m;

        for (var i = 0; i < intermediates.Count; i++)
        {
            var it = intermediates[i];
            var share = baseGlobal > 0 ? Round(descGlobal * it.subNeto / baseGlobal) : 0m;
            assigned += share;
            if (it.subNeto >= maxNeto)
            {
                maxNeto = it.subNeto;
                maxIdx = i;
            }

            results.Add(new VentaLineaDescuentoResult(
                it.subBruto,
                it.descLinea,
                it.subNeto,
                share,
                it.subNeto - share,
                it.ganAfterLine - share
            ));
        }

        var diff = descGlobal - assigned;
        if (diff != 0 && results.Count > 0)
        {
            var r = results[maxIdx];
            results[maxIdx] = r with
            {
                DescuentoGlobalAsignado = r.DescuentoGlobalAsignado + diff,
                SubtotalFinal = r.SubtotalFinal - diff,
                GananciaNetaEstimada = r.GananciaNetaEstimada - diff,
            };
        }

        return new VentaDescuentoResult(
            results.Sum(x => x.SubtotalBruto),
            baseGlobal,
            descGlobal,
            baseGlobal - descGlobal,
            results.Sum(x => x.GananciaNetaEstimada),
            results
        );
    }

    private static decimal Round(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

    private static decimal ClampPct(decimal p) => Math.Clamp(p, 0, 100);
}
