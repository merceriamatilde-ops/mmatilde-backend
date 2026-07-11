namespace MMatilde.Api.Models;

public class Usuario
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public RolUsuario Rol { get; set; } = RolUsuario.VIEWER;
    public bool Activo { get; set; } = true;
    /// <summary>Borrado lógico; null = cuenta vigente.</summary>
    public DateTime? EliminadoEn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
