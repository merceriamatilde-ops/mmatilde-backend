namespace MMatilde.Api.DTOs;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, Guid Id, string Email, string Nombre, string Rol);
