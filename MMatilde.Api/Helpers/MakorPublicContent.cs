namespace MMatilde.Api.Helpers;

using MMatilde.Api.Models;

public static class MakorPublicContent
{
    public static string SuggestTitle(string? nombre) => UnidadParser.StripUnidadSufijo(nombre);

    public static string? SuggestDescription(string? descripcion) =>
        string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim();

    public static bool IsDefaultTitle(string? nombrePublico, string? nombreAnterior, string? nombreNuevo)
    {
        if (string.IsNullOrWhiteSpace(nombrePublico)) return true;

        var stored = nombrePublico.Trim();
        foreach (var source in new[] { nombreAnterior, nombreNuevo })
        {
            if (string.IsNullOrWhiteSpace(source)) continue;
            var raw = source.Trim();
            var suggested = SuggestTitle(raw);
            if (stored == raw || stored == suggested || SuggestTitle(stored) == suggested) return true;
        }

        return false;
    }

    public static string ResolveTitle(string? nombre, string? nombrePublico)
    {
        var suggested = SuggestTitle(nombre);
        if (string.IsNullOrWhiteSpace(nombrePublico)) return suggested;
        if (IsDefaultTitle(nombrePublico, nombre, nombre)) return suggested;
        return nombrePublico.Trim();
    }

    public static bool IsDefaultDescription(string? descripcionPublica, string? descripcion)
    {
        if (string.IsNullOrWhiteSpace(descripcionPublica)) return true;
        if (string.IsNullOrWhiteSpace(descripcion)) return false;
        return descripcionPublica.Trim() == descripcion.Trim();
    }

    public static void ApplySyncedPublicFields(Producto producto, string nombreMakor, string? nombreAnterior, bool isNew)
    {
        if (isNew || IsDefaultTitle(producto.NombrePublico, nombreAnterior, nombreMakor))
            producto.NombrePublico = SuggestTitle(nombreMakor);

        if (isNew || IsDefaultDescription(producto.DescripcionPublica, producto.Descripcion))
            producto.DescripcionPublica = SuggestDescription(producto.Descripcion);
    }
}
