namespace MMatilde.Api.Models;

public class Tag
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? ColorHex { get; set; }
    public bool VisibleEnCatalogo { get; set; } = true;
    public int Orden { get; set; } = 0;
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ProductoTag> Productos { get; set; } = new List<ProductoTag>();
}
