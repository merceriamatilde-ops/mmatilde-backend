namespace MMatilde.Api.Models;

public class Producto
{
    public int Id { get; set; }
    public string CodigoMakor { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Composicion { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public decimal? PrecioMinorista { get; set; }
    public decimal DescuentoPorcentaje { get; set; } = 0;
    public bool Destacado { get; set; } = false;
    public bool Activo { get; set; } = false;
    public int CategoriaId { get; set; }
    public int? SubcategoriaId { get; set; }
    public int? MarcaId { get; set; }
    public int ProveedorId { get; set; }
    public DateTime? UltimaSync { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Categoria? Categoria { get; set; }
    public Subcategoria? Subcategoria { get; set; }
    public Marca? Marca { get; set; }
    public Proveedor? Proveedor { get; set; }
    public ICollection<ProductoVariante> Variantes { get; set; } = new List<ProductoVariante>();
    public ICollection<ProductoImagen> Imagenes { get; set; } = new List<ProductoImagen>();
    public ICollection<ProductoRelacionado> Relacionados { get; set; } = new List<ProductoRelacionado>();
}
