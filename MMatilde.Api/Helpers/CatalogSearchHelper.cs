using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MMatilde.Api.Helpers;

/// <summary>
/// Normaliza consultas de búsqueda del catálogo: sinónimos, typos frecuentes y tokens.
/// </summary>
public static class CatalogSearchHelper
{
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "para", "con", "sin", "del", "de", "la", "el", "los", "las", "una", "uno", "por",
        "mas", "más", "muy", "que", "como", "tipo", "aprox", "aproximado",
    };

    private static readonly Dictionary<string, string> TokenAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["semigorda"] = "semigruesa",
        ["semigroso"] = "semigruesa",
        ["semigrueso"] = "semigruesa",
        ["semi-fina"] = "semifina",
        ["semi fina"] = "semifina",
        ["semi-fino"] = "semifina",
        ["semi fino"] = "semifina",
        ["acrilica"] = "acrilico",
        ["acrílica"] = "acrilico",
        ["agujas"] = "aguja",
        ["tejedoras"] = "tejedor",
        ["tejedora"] = "tejedor",
        ["lanas"] = "lana",
        ["hilos"] = "hilo",
        ["guatas"] = "guata",
        ["cierres"] = "cierre",
        ["botones"] = "boton",
        ["botón"] = "boton",
        ["elasticos"] = "elastico",
        ["elástico"] = "elastico",
        ["elásticos"] = "elastico",
    };

    private static readonly Dictionary<string, string[]> SynonymExpansion = new(StringComparer.OrdinalIgnoreCase)
    {
        ["semigruesa"] = ["semigorda", "gruesa", "grueso"],
        ["semifina"] = ["fina", "fino"],
        ["lana"] = ["ovillo", "madeja"],
        ["hilo"] = ["ovillo", "madeja"],
        ["aguja"] = ["tejedor", "circular"],
        ["crochet"] = ["croche", "ganchillo"],
        ["algodon"] = ["algodón"],
        ["acrilico"] = ["acrílico", "sintetica", "sintética"],
        ["guata"] = ["relleno", "volumen"],
        ["cierre"] = ["zipper", "cremallera"],
    };

    public static string NormalizeQuery(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;

        var text = RemoveDiacritics(query.Trim().ToLowerInvariant());
        text = Regex.Replace(text, @"[-_/\\.,;:+]", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static string CanonicalToken(string token)
    {
        var normalized = NormalizeQuery(token);
        if (TokenAliases.TryGetValue(normalized, out var alias))
            return alias;
        return normalized;
    }

    public static List<string> ExpandSearchTokens(string query)
    {
        var normalized = NormalizeQuery(query);
        if (normalized.Length < 3) return [];

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddToken(string? token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            var canon = CanonicalToken(token);
            if (canon.Length < 3 || StopWords.Contains(canon)) return;
            if (!visited.Add(canon)) return;

            tokens.Add(canon);
            if (canon.EndsWith("s", StringComparison.Ordinal) && canon.Length > 4)
                tokens.Add(canon[..^1]);

            if (SynonymExpansion.TryGetValue(canon, out var syns))
            {
                foreach (var syn in syns)
                    AddToken(syn);
            }
        }

        AddToken(normalized);
        foreach (var part in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            AddToken(part);

        return tokens.Where(t => t.Length >= 3 && !StopWords.Contains(t)).ToList();
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
