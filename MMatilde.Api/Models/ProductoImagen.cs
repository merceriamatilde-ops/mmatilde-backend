namespace MMatilde.Api.Models;

public class ProductoImagen
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public int? VarianteId { get; set; }
    public string? CloudinaryPublicId { get; set; }
    public string? UrlOriginal { get; set; }
    public string? AltText { get; set; }
    public int Orden { get; set; } = 0;
    public bool EsPrincipal { get; set; } = false;
    public bool EsDeProveedor { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Producto? Producto { get; set; }
    public ProductoVariante? Variante { get; set; }
}
