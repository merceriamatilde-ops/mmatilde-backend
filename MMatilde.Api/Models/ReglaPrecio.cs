namespace MMatilde.Api.Models;

public class ReglaPrecio
{
    public int Id { get; set; }
    public int? CategoriaId { get; set; }
    public int? SubcategoriaId { get; set; }
    public int? MarcaId { get; set; }
    public decimal MargenPorcentaje { get; set; }
    public TipoPrecio Tipo { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
