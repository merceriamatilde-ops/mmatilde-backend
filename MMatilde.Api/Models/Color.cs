namespace MMatilde.Api.Models;

public class Color
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? CodigoHex { get; set; }
    public string Slug { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
