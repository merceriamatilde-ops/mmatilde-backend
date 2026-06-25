namespace MMatilde.Api.DTOs;

public record HomeDataDto(List<CategoriaCardDto> Categorias, List<ProductoCatalogoDto> ProductosRecientes);
public record CategoriaCardDto(string Nombre, string Icon, string Slug);
public record DashboardStatsDto(int TotalProductos, int ProductosActivos, int TotalCategorias);
