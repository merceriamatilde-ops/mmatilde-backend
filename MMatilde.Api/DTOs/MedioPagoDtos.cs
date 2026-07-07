namespace MMatilde.Api.DTOs;

public record MedioPagoDto(
    int Id,
    string Nombre,
    string Slug,
    bool Activo,
    bool EsDefault,
    int Orden
);

public record MedioPagoCreateDto(string Nombre, int Orden, bool Activo);

public record MedioPagoUpdateDto(string Nombre, int Orden, bool Activo);
