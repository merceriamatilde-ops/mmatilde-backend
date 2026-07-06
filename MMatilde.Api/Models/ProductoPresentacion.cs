namespace MMatilde.Api.Models;

/// <summary>Forma en que se vende un producto (ej. 100 g, por metro, x10 unidades).</summary>
public class ProductoPresentacion
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Nombre { get; set; } = string.Empty;
    /// <summary>Cantidad en la unidad base del producto (ej. 100 si la base es gramos).</summary>
    public decimal CantidadUnidadBase { get; set; } = 1;
    public decimal? PrecioVenta { get; set; }
    /// <summary>Margen % sobre costo+IVA. Si null, usa regla global/categoría.</summary>
    public decimal? MargenPorcentaje { get; set; }
    public bool EsDefault { get; set; } = false;
    public bool Activo { get; set; } = true;
    public int Orden { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Producto? Producto { get; set; }
}
