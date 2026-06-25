namespace MMatilde.Api.DTOs;

public record ProductoAdminDto(int Id, string CodigoMakor, string Nombre, string Categoria, decimal? PrecioMayorista, decimal? PrecioMinorista, bool Activo, bool Destacado, DateTime? UltimaSync);
public record ProductoAdminListResponse(List<ProductoAdminDto> Items, int Total, int Page, int PageSize, int TotalPages);
public record ProductoCatalogoDto(int Id, string Slug, string Nombre, string Categoria, string? ImagenUrl);
public record ProductoDetalleDto(int Id, string Slug, string Nombre, string? Descripcion, string Categoria, string CategoriaSlug, List<string> Imagenes);
public record ToggleRequest(bool Value);
public record BulkToggleRequest(List<int> Ids, bool Activo);

public record ProductoCreateDto(string Nombre, string? Codigo, int CategoriaId, int? SubcategoriaId, string? Descripcion, decimal? PrecioBase, bool Destacado, bool Visible, string? ImagenUrl);
public record ProductoUpdateDto(string Nombre, string? Codigo, int CategoriaId, int? SubcategoriaId, string? Descripcion, decimal? PrecioBase, bool Destacado, bool Visible, string? ImagenUrl);
