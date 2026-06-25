namespace MMatilde.Api.Models;

public class Subcategoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public int CategoriaId { get; set; }
    public int Orden { get; set; } = 0;
    public bool EsMakor { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Categoria? Categoria { get; set; }
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
