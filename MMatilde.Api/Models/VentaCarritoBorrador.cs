namespace MMatilde.Api.Models;

public class VentaCarritoBorrador
{
    public Guid UsuarioId { get; set; }
    public Usuario Usuario { get; set; } = null!;
    public string PayloadJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
