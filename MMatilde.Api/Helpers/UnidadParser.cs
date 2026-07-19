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
    private const RegexOptions Rx = RegexOptions.IgnoreCase;

    private static readonly (Regex Pattern, Func<Match, UnidadDetectada?> Build)[] Rules =
    [
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*kg\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.g, n.Value * 1000m, $"{Fmt(n)} kg", true);
        }),
        (new Regex(@"(\d+(?:[.,]\d+)?)\s*kg\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.g, n.Value * 1000m, $"{Fmt(n)} kg", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*(?:g|gr|gramos?)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.g, n.Value, $"{Fmt(n)} g", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*(?:m|mt|mts|metro|metros)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.m, n.Value, $"{Fmt(n)} m", true);
        }),
        (new Regex(@"(\d+(?:[.,]\d+)?)\s*(?:m|mt|mts|metro|metros)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.m, n.Value, $"{Fmt(n)} m", true);
        }),
        (new Regex(@"rollo\s*(\d+(?:[.,]\d+)?)\s*(?:m|mt|mts|metro|metros)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.m, n.Value, $"rollo {Fmt(n)} m", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*cm\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.cm, n.Value, $"{Fmt(n)} cm", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*l(?:itros?)?\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.ml, n.Value * 1000m, $"{Fmt(n)} l", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*ml\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.ml, n.Value, $"{Fmt(n)} ml", true);
        }),
        (new Regex(@"\bdocena\b", Rx), _ =>
            new UnidadDetectada(UnidadMedida.unidad, 12m, "docena (12 u)", true)),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*un\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.unidad, n.Value, $"{Fmt(n)} u", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+(?:[.,]\d+)?)\s*(?:u(?:n(?:idad(?:es)?)?)?|pzas?|piezas?)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.unidad, n.Value, $"{Fmt(n)} u", true);
        }),
        (new Regex(@"(\d+(?:[.,]\d+)?)\s*(?:u(?:n(?:idad(?:es)?)?)?|pzas?|piezas?)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.unidad, n.Value, $"{Fmt(n)} u", true);
        }),
        (new Regex(@"(?<=\s)x\s*(\d+)\b", Rx), m =>
        {
            var n = ParseDecimal(m.Groups[1].Value);
            return n is null ? null : new UnidadDetectada(UnidadMedida.unidad, n.Value, $"x{Fmt(n)}", false);
        }),
    ];

    public static UnidadDetectada? TryParse(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return null;

        UnidadDetectada? best = null;
        var bestIndex = -1;

        foreach (var (pattern, build) in Rules)
        {
            foreach (Match match in pattern.Matches(nombre))
            {
                var result = build(match);
                if (result == null) continue;

                if (IsBetterMatch(match, result, bestIndex, best))
                {
                    best = result;
                    bestIndex = match.Index;
                }
            }
        }

        return best;
    }

    /// <summary>Fallback cuando el título no trae medida: 1 unidad. Permite que la fórmula calcule igual.</summary>
    public static UnidadDetectada DefaultUnidad { get; } =
        new(UnidadMedida.unidad, 1m, "1 u", false);

    public static UnidadDetectada ParseOrDefault(string? nombre) =>
        TryParse(nombre) ?? DefaultUnidad;

    public static bool TryApplyTo(Producto producto, string? nombre)
    {
        var detected = TryParse(nombre);
        if (detected == null) return false;

        Apply(producto, detected);
        return true;
    }

    /// <summary>
    /// Detecta del título; si no hay nada, pone unidad base = Unidad y cantidad = 1.
    /// Devuelve true si se aplicó el fallback (no había match en el título).
    /// </summary>
    public static bool ApplyDetectedOrDefault(Producto producto, string? nombre)
    {
        var detected = TryParse(nombre);
        var usedDefault = detected == null;
        Apply(producto, detected ?? DefaultUnidad);
        return usedDefault;
    }

    private static void Apply(Producto producto, UnidadDetectada detected)
    {
        producto.UnidadBase = detected.UnidadBase;
        producto.CantidadUnidadCompra = detected.CantidadUnidadCompra;
        producto.EtiquetaUnidadCompra = detected.Etiqueta;
        producto.UnidadCompraAutoDetectada = true;
    }

    public static string StripUnidadSufijo(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return nombre ?? string.Empty;

        var trimmed = nombre.Trim();
        Match? bestMatch = null;
        UnidadDetectada? best = null;

        foreach (var (pattern, build) in Rules)
        {
            foreach (Match match in pattern.Matches(trimmed))
            {
                if (match.Index + match.Length != trimmed.Length) continue;

                var result = build(match);
                if (result == null) continue;

                if (best == null
                    || (result.Confiable && !best.Confiable)
                    || (result.Confiable == best.Confiable && match.Length > (bestMatch?.Length ?? 0)))
                {
                    best = result;
                    bestMatch = match;
                }
            }
        }

        if (bestMatch == null)
        {
            var orphanX = Regex.Match(trimmed, @"\s+x\s*$", Rx);
            if (orphanX.Success)
            {
                var head = trimmed[..orphanX.Index].TrimEnd();
                return string.IsNullOrWhiteSpace(head) ? trimmed : head.TrimEnd('-', '–', '—', ' ').Trim();
            }
            return trimmed;
        }

        var headText = trimmed[..bestMatch.Index].TrimEnd();
        if (string.IsNullOrWhiteSpace(headText)) return trimmed;

        return headText.TrimEnd('-', '–', '—', ' ').Trim();
    }

    public static bool EsProductoEnMetros(Producto producto) =>
        producto.UnidadBase == UnidadMedida.m ||
        (producto.UnidadBase == UnidadMedida.cm &&
         !string.IsNullOrWhiteSpace(producto.EtiquetaUnidadCompra) &&
         Regex.IsMatch(producto.EtiquetaUnidadCompra, @"\b(?:m|mt|mts|metro|metros)\b", Rx));

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

    private static bool IsBetterMatch(Match match, UnidadDetectada candidate, int bestIndex, UnidadDetectada? best)
    {
        if (best == null) return true;

        if (candidate.Confiable && !best.Confiable) return true;
        if (!candidate.Confiable && best.Confiable) return false;

        return match.Index > bestIndex;
    }

    private static decimal? ParseDecimal(string raw)
    {
        var normalized = raw.Replace(',', '.');
        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var n) ? n : null;
    }

    private static string Fmt(decimal? n) =>
        n?.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) ?? "";
}
