namespace MMatilde.Api.Models;

public class SyncLog
{
    public int Id { get; set; }
    public int ProveedorId { get; set; }
    public EstadoSync Estado { get; set; } = EstadoSync.PENDIENTE;
    public int ProductosNuevos { get; set; } = 0;
    public int ProductosActualizados { get; set; } = 0;
    public int Errores { get; set; } = 0;
    public string? TermsJson { get; set; }
    public string? CategoriasJson { get; set; }
    public string? ResumenJson { get; set; }
    public string? DetalleErrores { get; set; }
    public DateTime IniciadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? FinalizadoEn { get; set; }

    public Proveedor? Proveedor { get; set; }
}
