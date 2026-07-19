namespace MMatilde.Api.Models;

public enum BannerLinkTipo
{
    Ninguno,
    Categoria,
    Coleccion,
    Url
}

public class Banner
{
    public int Id { get; set; }

    /// <summary>Referencia interna para el BO, no se muestra al público.</summary>
    public string Titulo { get; set; } = string.Empty;

    public string ImagenDesktopUrl { get; set; } = string.Empty;

    /// <summary>Opcional; si está vacío se usa la imagen desktop.</summary>
    public string? ImagenMobileUrl { get; set; }

    public BannerLinkTipo LinkTipo { get; set; } = BannerLinkTipo.Ninguno;
    public int? LinkCategoriaId { get; set; }
    public int? LinkTagId { get; set; }
    public string? LinkUrl { get; set; }

    /// <summary>Dónde se muestra. Por ahora "home"; string para abrir a futuro sin migración.</summary>
    public string Ubicacion { get; set; } = "home";

    public int Orden { get; set; } = 0;
    public bool Activo { get; set; } = true;
    public bool AbreEnNuevaPestana { get; set; } = false;

    public DateTime? FechaDesde { get; set; }
    public DateTime? FechaHasta { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Categoria? LinkCategoria { get; set; }
    public Tag? LinkTag { get; set; }
}
