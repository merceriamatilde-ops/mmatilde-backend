namespace MMatilde.Api.Models;

/// <summary>Registro de venta (módulo en construcción).</summary>
public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public decimal Total { get; set; }
    public string? Notas { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VentaLinea> Lineas { get; set; } = new List<VentaLinea>();
}

/// <summary>Línea de venta con snapshot económico al momento de la operación.</summary>
public class VentaLinea
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int ProductoId { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitarioVenta { get; set; }
    public ModoOrigenEconomico ModoOrigenEconomico { get; set; }
    public decimal? CostoCompraSnapshot { get; set; }
    public decimal? CostoMaterialesSnapshot { get; set; }
    public decimal? ManoObraSnapshot { get; set; }
    public decimal? ComisionTiendaPorcentajeSnapshot { get; set; }
    /// <summary>Ganancia neta estimada para la mercería en esta línea.</summary>
    public decimal GananciaNetaEstimada { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Venta? Venta { get; set; }
    public Producto? Producto { get; set; }
}
