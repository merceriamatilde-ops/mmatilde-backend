namespace MMatilde.Api.DTOs;

public record TurnoVentaDto(
    int Id,
    string Slug,
    string Nombre,
    int Orden,
    bool Activo,
    string HoraDesde,
    string DescripcionHorario
);

public record TurnoVentaCreateDto(string Nombre, int Orden, bool Activo, string HoraDesde);

public record TurnoVentaUpdateDto(string Nombre, int Orden, bool Activo, string HoraDesde);
