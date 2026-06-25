namespace MMatilde.Api.Models;

public class Categoria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public string? Icono { get; set; }
    public string? Imagen { get; set; }
    public int Orden { get; set; } = 0;
    public bool Activo { get; set; } = true;
    public bool EsMakor { get; set; } = true; // Por defecto true para no romper existentes
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Subcategoria> Subcategorias { get; set; } = new List<Subcategoria>();
    public ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
