namespace MMatilde.Api.Services;

public static class IaMatching
{
    public static string NormalizeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        var lower = text.ToLowerInvariant();
        return lower
            .Replace('á', 'a').Replace('é', 'e').Replace('í', 'i')
            .Replace('ó', 'o').Replace('ú', 'u').Replace('ñ', 'n');
    }

    public static int ScoreDisparadores(string disparadores, string textoNormalizado)
    {
        if (string.IsNullOrWhiteSpace(textoNormalizado))
            return string.IsNullOrWhiteSpace(disparadores) ? 1 : 0;

        if (string.IsNullOrWhiteSpace(disparadores))
            return 1;

        var score = 0;
        foreach (var term in disparadores.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var norm = NormalizeText(term);
            if (norm.Length >= 2 && textoNormalizado.Contains(norm, StringComparison.Ordinal))
                score += 2;
        }

        return score;
    }

    public static int ScoreEjemplo(string disparadores, string descripcion, string titulo, string textoNormalizado)
    {
        if (string.IsNullOrWhiteSpace(textoNormalizado))
            return string.IsNullOrWhiteSpace(disparadores) ? 1 : 0;

        return ScoreDisparadores(disparadores, textoNormalizado)
             + ScoreDisparadores(descripcion, textoNormalizado)
             + ScoreDisparadores(titulo, textoNormalizado);
    }
}
