namespace MMatilde.Api.DTOs;

// Público — el href ya viene resuelto por el backend.
public record BannerPublicoDto(
    int Id,
    string ImagenDesktopUrl,
    string? ImagenMobileUrl,
    string? Href,
    bool EsExterno,
    bool AbreEnNuevaPestana,
    string Titulo
);

public record BannerAdminDto(
    int Id,
    string Titulo,
    string ImagenDesktopUrl,
    string? ImagenMobileUrl,
    string LinkTipo,
    int? LinkCategoriaId,
    int? LinkTagId,
    string? LinkUrl,
    string? Href,
    string Ubicacion,
    int Orden,
    bool Activo,
    bool AbreEnNuevaPestana,
    DateTime? FechaDesde,
    DateTime? FechaHasta,
    bool Vigente
);

public record BannerUpsertDto(
    string Titulo,
    string ImagenDesktopUrl,
    string? ImagenMobileUrl,
    string LinkTipo,
    int? LinkCategoriaId,
    int? LinkTagId,
    string? LinkUrl,
    string? Ubicacion,
    bool Activo,
    bool AbreEnNuevaPestana,
    DateTime? FechaDesde,
    DateTime? FechaHasta
);

public record BannerReorderDto(List<int> Ids);
