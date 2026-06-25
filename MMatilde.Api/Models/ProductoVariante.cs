namespace MMatilde.Api.Models;

public class ProductoVariante
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? ColorId { get; set; }
    public string? Talle { get; set; }
    public string? Medida { get; set; }
    public string? CodigoArticulo { get; set; }
    public bool Activo { get; set; } = false;
    public int Orden { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Producto? Producto { get; set; }
    public Color? Color { get; set; }
    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
}
