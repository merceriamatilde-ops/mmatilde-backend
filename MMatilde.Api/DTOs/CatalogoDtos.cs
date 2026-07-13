namespace MMatilde.Api.DTOs;

public record HomeDataDto(
    List<CategoriaCardDto> Categorias,
    List<ProductoCatalogoDto> ProductosRecientes,
    List<ColeccionCardDto> Colecciones,
    int MaxCategoriasMobile,
    int MaxCategoriasDesktop
);
public record CategoriaCardDto(string Nombre, string Icon, string Slug, int Count, string? Imagen);
public record DashboardStatsDto(int TotalProductos, int ProductosActivos, int TotalCategorias);
