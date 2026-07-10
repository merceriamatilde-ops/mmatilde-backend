using MMatilde.Api.DTOs;
using MMatilde.Api.Models;

namespace MMatilde.Api.Helpers;

public static class ProductoDisplay
{
    public static string NombrePublico(Producto p) =>
        p.ProveedorId == 1
            ? MakorPublicContent.ResolveTitle(p.Nombre, p.NombrePublico)
            : !string.IsNullOrWhiteSpace(p.NombrePublico)
                ? p.NombrePublico!.Trim()
                : p.Nombre;

    public static ProductoCatalogoDto ToCatalogoDto(Producto p, string? categoriaNombre = null) =>
        new(
            p.Id,
            p.Slug,
            NombrePublico(p),
            categoriaNombre ?? p.Categoria?.Nombre ?? "",
            ImagenPublica(p)
        );

    public static string? DescripcionPublica(Producto p) =>
        !string.IsNullOrWhiteSpace(p.DescripcionPublica) ? p.DescripcionPublica : p.Descripcion;

    public static string? ImagenPublica(Producto p)
    {
        if (!string.IsNullOrWhiteSpace(p.ImagenPublicaUrl))
            return p.ImagenPublicaUrl;

        var imagenes = p.Imagenes?.OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden).ToList()
            ?? new List<ProductoImagen>();

        return imagenes.FirstOrDefault(i => !i.EsDeProveedor)?.UrlOriginal
            ?? imagenes.FirstOrDefault(i => i.EsDeProveedor)?.UrlOriginal
            ?? imagenes.FirstOrDefault()?.UrlOriginal;
    }

    public static List<string> ImagenesPublicas(Producto p)
    {
        if (!string.IsNullOrWhiteSpace(p.ImagenPublicaUrl))
            return new List<string> { p.ImagenPublicaUrl };

        var imagenes = p.Imagenes?.OrderByDescending(i => i.EsPrincipal).ThenBy(i => i.Orden).ToList()
            ?? new List<ProductoImagen>();

        var propias = imagenes.Where(i => !i.EsDeProveedor).Select(i => i.UrlOriginal!).Where(u => u != null).ToList();
        if (propias.Count > 0) return propias;

        return imagenes.Where(i => i.EsDeProveedor).Select(i => i.UrlOriginal!).Where(u => u != null).ToList();
    }

    public static string? ImagenProveedor(Producto p) =>
        p.Imagenes?
            .Where(i => i.EsDeProveedor)
            .OrderByDescending(i => i.EsPrincipal)
            .ThenBy(i => i.Orden)
            .FirstOrDefault()?.UrlOriginal;
}
