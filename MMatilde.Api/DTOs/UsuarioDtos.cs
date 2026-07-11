namespace MMatilde.Api.DTOs;

public record UsuarioListDto(
    Guid Id,
    string Email,
    string Nombre,
    string Rol,
    bool Activo,
    DateTime CreatedAt
);

public record UsuarioCreateDto(
    string Email,
    string Nombre,
    string Password,
    string Rol
);

public record UsuarioUpdateDto(
    string Email,
    string Nombre,
    string Rol,
    bool Activo
);

public record UsuarioPasswordDto(string Password);
