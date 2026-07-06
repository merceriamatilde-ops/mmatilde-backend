namespace MMatilde.Api.Models;

public enum RolUsuario { ADMIN, VIEWER }
public enum EstadoSync { PENDIENTE, EN_PROCESO, COMPLETADO, ERROR }
public enum TipoPrecio { MARKUP_GLOBAL, MARKUP_CATEGORIA, DESCUENTO }

/// <summary>Cómo se determina el precio de venta del producto.</summary>
public enum ModoPrecio
{
    /// <summary>Usa reglas de categoría / margen global.</summary>
    AUTOMATICO,
    /// <summary>IVA y margen propios del producto (sigue calculando desde costo).</summary>
    EXCEPCION,
    /// <summary>Precio final manual; no recalcula desde costo Makor.</summary>
    PRECIO_FIJO
}

/// <summary>Origen económico del producto: define cómo se calcula la ganancia neta.</summary>
public enum ModoOrigenEconomico
{
    /// <summary>Compra para revender (Makor, mayorista, etc.).</summary>
    REVENTA,
    /// <summary>Producto de tercero; la mercería cobra un % al vender.</summary>
    CONSIGNACION,
    /// <summary>Elaboración propia: materiales + mano de obra.</summary>
    ELABORACION_PROPIA,
    /// <summary>Sin costo de adquisición (regalo, donación).</summary>
    SIN_COSTO
}

/// <summary>Unidad mínima para cálculos de costo y presentaciones de venta.</summary>
public enum UnidadMedida
{
    g,
    kg,
    cm,
    m,
    ml,
    l,
    unidad,
    par,
    docena
}
