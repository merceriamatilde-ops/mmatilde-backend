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

        var destBebe = ContieneAlguno(textoNormalizado, "bebe", "recien nacido", "nino", "infantil");
        var destMascota = ContieneAlguno(textoNormalizado, "mascota", "perro", "gato", "caniche");
        var destNoAdulto = destBebe || destMascota;
        var dispNorm = NormalizeText(disparadores);
        var reglaAdulto = ContieneAlguno(dispNorm, "adulto", "adulta");
        var reglaBebe = ContieneAlguno(dispNorm, "bebe", "recien nacido", "nino", "infantil");
        var reglaMascota = ContieneAlguno(dispNorm, "mascota", "perro", "gato");

        // Evita que "bufanda, lana" (adulto implícito) pise un pedido de bebé/mascota.
        if (destNoAdulto && reglaAdulto)
            return 0;
        if (destBebe && !reglaBebe && !reglaMascota && ContieneAlguno(dispNorm, "bufanda", "chal", "adulto"))
            return 0;
        if (destMascota && !reglaMascota && ContieneAlguno(dispNorm, "bufanda", "chal", "adulto"))
            return 0;

        var score = 0;
        foreach (var term in disparadores.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var norm = NormalizeText(term);
            if (norm.Length >= 2 && textoNormalizado.Contains(norm, StringComparison.Ordinal))
                score += 2;
        }

        return score;
    }

    private static bool ContieneAlguno(string haystack, params string[] needles)
    {
        foreach (var n in needles)
        {
            var norm = NormalizeText(n);
            if (norm.Length >= 3 && haystack.Contains(norm, StringComparison.Ordinal))
                return true;
        }
        return false;
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
