namespace MMatilde.Api.Models;

/// <summary>Registro de venta (módulo en construcción).</summary>
public class Venta
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = DateTime.UtcNow;
    public string Turno { get; set; } = "MANANA";
    /// <summary>Slug del medio de pago (tabla medios_pago).</summary>
    public string MedioPagoSlug { get; set; } = string.Empty;
    public decimal SubtotalBruto { get; set; }
    public decimal DescuentoGlobalPorcentaje { get; set; }
    public decimal DescuentoGlobalMonto { get; set; }
    public decimal Total { get; set; }
    public decimal GananciaNetaEstimada { get; set; }
    public string? Notas { get; set; }
    public Guid? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    /// <summary>Snapshot del nombre al crear la venta (persiste si el usuario se archiva).</summary>
    public string? UsuarioNombre { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<VentaLinea> Lineas { get; set; } = new List<VentaLinea>();
}

/// <summary>Línea de venta con snapshot económico al momento de la operación.</summary>
public class VentaLinea
{
    public int Id { get; set; }
    public int VentaId { get; set; }
    public int? ProductoId { get; set; }
    public int? VarianteId { get; set; }
    public string? VarianteLabel { get; set; }
    public int? PresentacionId { get; set; }
    public string? PresentacionNombre { get; set; }
    public string ProductoNombre { get; set; } = string.Empty;
    /// <summary>Descripción de qué se vendió (líneas "Varios").</summary>
    public string? NotaLinea { get; set; }
    public decimal Cantidad { get; set; } = 1;
    public decimal PrecioUnitarioVenta { get; set; }
    public decimal SubtotalBruto { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal DescuentoMonto { get; set; }
    public decimal DescuentoGlobalAsignado { get; set; }
    public decimal Subtotal { get; set; }
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
