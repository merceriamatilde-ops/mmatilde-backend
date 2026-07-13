namespace MMatilde.Api.DTOs;

public record CategoriaConConteoDto(int Id, string Nombre, string Slug, string? Icono, int Count, string? Imagen);

public record SubcategoriaAdminDto(int Id, string Nombre, string Slug, bool EsMakor, int Count);
public record CategoriaAdminDto(int Id, string Nombre, string Slug, bool EsMakor, int Count, List<SubcategoriaAdminDto> Subcategorias, string? Imagen, string? Icono);

public record CategoriaCreateDto(string Nombre);
public record CategoriaUpdateDto(string Nombre);
public record CategoriaImagenDto(string? Imagen);
public record CategoriaReorderDto(List<int> Ids);

public record SubcategoriaCreateDto(string Nombre, int CategoriaId);
public record SubcategoriaUpdateDto(string Nombre);

public record SubcategoriaCatalogoDto(int Id, string Nombre, string Slug);
public record CategoriaCatalogoResponseDto(int Id, string Nombre, string Slug, string? Icono, List<SubcategoriaCatalogoDto> Subcategorias, List<ProductoCatalogoDto> Productos, string? Imagen);
