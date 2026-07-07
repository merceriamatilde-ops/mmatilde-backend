namespace MMatilde.Api.Models;

/// <summary>Configuración de turno operativo para ventas (mañana, tarde, etc.).</summary>
public class TurnoVentaConfig
{
    public int Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    /// <summary>Hora local (Argentina) desde la cual aplica este turno, inclusive.</summary>
    public TimeOnly HoraDesde { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
