namespace MMatilde.Api.Models;

public class IaEjemplo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Disparadores { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public string RespuestaJson { get; set; } = "{}";
    public string? ImagenUrl { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
}
