namespace MMatilde.Api.Models;

public class ConfiguracionSitio
{
    public int Id { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Tipo { get; set; } = "text";
    public string Grupo { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Orden { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
