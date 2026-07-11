namespace MMatilde.Api.Models;

public class Producto
{
    public int Id { get; set; }
    public string CodigoMakor { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? NombrePublico { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? DescripcionPublica { get; set; }
    public string? ImagenPublicaUrl { get; set; }
    public string? Composicion { get; set; }
    public decimal? PrecioMayorista { get; set; }
    public decimal? PrecioMinorista { get; set; }
    public decimal DescuentoPorcentaje { get; set; } = 0;

    /// <summary>Unidad mínima para cálculos (g, cm, unidad, etc.).</summary>
    public UnidadMedida? UnidadBase { get; set; }
    /// <summary>Cuántas unidades base trae un paquete de compra (ej. 1000 g = 1 kg).</summary>
    public decimal? CantidadUnidadCompra { get; set; }
    /// <summary>Etiqueta legible de la unidad de compra (ej. "1 kg", "rollo 10 m").</summary>
    public string? EtiquetaUnidadCompra { get; set; }
    public bool UnidadCompraAutoDetectada { get; set; } = false;

    public ModoPrecio ModoPrecio { get; set; } = ModoPrecio.AUTOMATICO;
    /// <summary>Origen económico: reventa, consignación, elaboración propia o sin costo.</summary>
    public ModoOrigenEconomico ModoOrigenEconomico { get; set; } = ModoOrigenEconomico.REVENTA;
    /// <summary>IVA % propio cuando ModoPrecio es EXCEPCION o PRECIO_FIJO.</summary>
    public decimal? IvaPorcentajeProducto { get; set; }
    /// <summary>Margen % propio cuando ModoPrecio es EXCEPCION.</summary>
    public decimal? MargenPorcentajeProducto { get; set; }
    /// <summary>Consignación: % que retiene la mercería sobre el precio de venta.</summary>
    public decimal? ComisionTiendaPorcentaje { get; set; }
    /// <summary>Consignación: nombre del titular del producto (ej. tía, artesana).</summary>
    public string? TitularConsignacion { get; set; }
    /// <summary>Elaboración propia: costo de materiales/insumos.</summary>
    public decimal? CostoMateriales { get; set; }
    /// <summary>Elaboración propia: costo de mano de obra (no es ganancia).</summary>
    public decimal? ManoObra { get; set; }
    /// <summary>Elaboración propia: margen % sobre materiales + mano de obra.</summary>
    public decimal? MargenElaboracionPorcentaje { get; set; }
    /// <summary>Elaboración propia: margen fijo en $ (ganancia de la mercería).</summary>
    public decimal? MargenElaboracionMonto { get; set; }

    public bool Destacado { get; set; } = false;
    /// <summary>Producto genérico para ventas sin catálogo (ej. "Varios"). Excluido de rankings.</summary>
    public bool EsVentaLibre { get; set; } = false;
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
    public ICollection<ProductoPresentacion> Presentaciones { get; set; } = new List<ProductoPresentacion>();
    public ICollection<ProductoTag> Tags { get; set; } = new List<ProductoTag>();
}
