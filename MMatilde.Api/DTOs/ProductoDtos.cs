namespace MMatilde.Api.DTOs;

public record ProductoAdminDto(int Id, string CodigoMakor, string Nombre, string Categoria, decimal? PrecioMayorista, decimal? PrecioMinorista, bool Activo, bool Destacado, DateTime? UltimaSync);
public record ProductoAdminListResponse(List<ProductoAdminDto> Items, int Total, int Page, int PageSize, int TotalPages);
public record ProductoCatalogoDto(int Id, string Slug, string Nombre, string Categoria, string? ImagenUrl);
public record ProductoDetalleDto(int Id, string Slug, string Nombre, string? Descripcion, string Categoria, string CategoriaSlug, string? Subcategoria, string? SubcategoriaSlug, List<string> Imagenes, List<VarianteResponseDto>? Variantes = null, List<ProductoRelacionadoDto>? Relacionados = null);
public record VarianteResponseDto(int Id, int? ColorId, string? ColorNombre, string? ColorHex, string? Talle, string? Medida, string? CodigoArticulo, bool Activo);
public record ProductoRelacionadoDto(int Id, string Nombre, string Slug, string? ImagenUrl);

public record ToggleRequest(bool Value);
public record BulkToggleRequest(List<int> Ids, bool Activo);

public record ProductoCreateDto(string Nombre, string? Codigo, int CategoriaId, int? SubcategoriaId, string? Descripcion, decimal? PrecioBase, bool Destacado, bool Visible, string? ImagenUrl, List<VarianteDto>? Variantes = null, List<int>? RelacionadosIds = null);
public record ProductoUpdateDto(string Nombre, string? Codigo, int CategoriaId, int? SubcategoriaId, string? Descripcion, decimal? PrecioBase, bool Destacado, bool Visible, string? ImagenUrl, List<VarianteDto>? Variantes = null, List<int>? RelacionadosIds = null);
public record VarianteDto(int? Id, int? ColorId, string? Talle, string? Medida, string? CodigoArticulo, bool Activo, int Orden);
