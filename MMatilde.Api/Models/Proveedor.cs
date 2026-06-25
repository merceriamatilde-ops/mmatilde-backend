namespace MMatilde.Api.Models;

public class Proveedor
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? UrlBase { get; set; }
    public string? ScrapingConfig { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime? UltimaSync { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
    public ICollection<SyncLog> SyncLogs { get; set; } = new List<SyncLog>();
}
