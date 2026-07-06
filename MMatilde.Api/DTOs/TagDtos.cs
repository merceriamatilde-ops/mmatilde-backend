namespace MMatilde.Api.DTOs;

public record TagDto(
    int Id,
    string Nombre,
    string Slug,
    string? Descripcion,
    string? ColorHex,
    bool VisibleEnCatalogo,
    int Orden,
    bool Activo,
    int ProductosCount
);

public record TagCreateDto(
    string Nombre,
    string? Descripcion,
    string? ColorHex,
    bool VisibleEnCatalogo,
    int Orden,
    bool Activo
);

public record TagUpdateDto(
    string Nombre,
    string? Descripcion,
    string? ColorHex,
    bool VisibleEnCatalogo,
    int Orden,
    bool Activo
);

public record ColeccionCardDto(string Nombre, string Slug, string? Descripcion, string? ColorHex, int Count);

public record ColeccionCategoriaFiltroDto(int Id, string Nombre, string Slug, int Count);

public record ColeccionDetalleDto(
    string Nombre,
    string Slug,
    string? Descripcion,
    string? ColorHex,
    List<ColeccionCategoriaFiltroDto> Categorias,
    List<ProductoCatalogoDto> Productos
);

public record TagResumenDto(int Id, string Nombre, string Slug);
