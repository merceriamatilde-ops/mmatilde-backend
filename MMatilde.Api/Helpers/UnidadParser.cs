using System.Text.RegularExpressions;
using MMatilde.Api.Models;

namespace MMatilde.Api.Helpers;

public record UnidadDetectada(
    UnidadMedida UnidadBase,
    decimal CantidadUnidadCompra,
    string Etiqueta,
    bool Confiable
);

public static class UnidadParser
{
    private static readonly (Regex Pattern, Func<Match, UnidadDetectada?> Build)[] Rules =
    [
        (new Regex(@"x\s*(\d+(?:[.,]\d+)?)\s*kg", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.g, n.Value * 1000m, $"{Fmt(n)} kg", true);
        }),
        (new Regex(@"(\d+(?:[.,]\d+)?)\s*kg", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.g, n.Value * 1000m, $"{Fmt(n)} kg", true);
        }),
        (new Regex(@"x\s*(\d+(?:[.,]\d+)?)\s*g(?:r(?:amos?)?)?\b", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.g, n.Value, $"{Fmt(n)} g", true);
        }),
        (new Regex(@"x\s*(\d+(?:[.,]\d+)?)\s*m(?:etros?)?\b", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.cm, n.Value * 100m, $"{Fmt(n)} m", true);
        }),
        (new Regex(@"rollo\s*(\d+(?:[.,]\d+)?)\s*m", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.cm, n.Value * 100m, $"rollo {Fmt(n)} m", true);
        }),
        (new Regex(@"x\s*(\d+(?:[.,]\d+)?)\s*cm", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.cm, n.Value, $"{Fmt(n)} cm", true);
        }),
        (new Regex(@"x\s*(\d+(?:[.,]\d+)?)\s*l(?:itros?)?\b", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.ml, n.Value * 1000m, $"{Fmt(n)} l", true);
        }),
        (new Regex(@"x\s*(\d+(?:[.,]\d+)?)\s*ml", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.ml, n.Value, $"{Fmt(n)} ml", true);
        }),
        (new Regex(@"\bdocena\b", RegexOptions.IgnoreCase), _ =>
            new UnidadDetectada(UnidadMedida.unidad, 12m, "docena (12 u)", true)),
        (new Regex(@"x\s*(\d+)\s*(?:u(?:n(?:idad(?:es)?)?)?|pzas?|piezas?)\b", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.unidad, n.Value, $"{Fmt(n)} u", true);
        }),
        (new Regex(@"x\s*(\d+)\b", RegexOptions.IgnoreCase), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.unidad, n.Value, $"x{Fmt(n)}", false);
        }),
    ];

    public static UnidadDetectada? TryParse(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;

        foreach (var (pattern, build) in Rules)
        {
            var match = pattern.Match(nombre);
            if (match.Success)
            {
                var result = build(match);
                if (result != null) return result;
            }
        }

        return null;
    }

    public static string LabelUnidad(UnidadMedida unidad) => unidad switch
    {
        UnidadMedida.g => "gramos",
        UnidadMedida.kg => "kilogramos",
        UnidadMedida.cm => "centímetros",
        UnidadMedida.m => "metros",
        UnidadMedida.ml => "mililitros",
        UnidadMedida.l => "litros",
        UnidadMedida.unidad => "unidades",
        UnidadMedida.par => "pares",
        UnidadMedida.docena => "docenas",
        _ => unidad.ToString()
    };

    private static decimal? ParseDecimal(string raw)
    {
        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static string Fmt(decimal? n) =>
        n?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "";
}
