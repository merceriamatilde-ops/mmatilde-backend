namespace MMatilde.Api.Models;

public class IaReglaAprendida
{
    public int Id { get; set; }
    public string Titulo { get; set; } = "";
    public string Disparadores { get; set; } = "";
    public string Regla { get; set; } = "";
    public bool Activa { get; set; } = true;
    public int? ConsultaOrigenId { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;

    public IaConsulta? ConsultaOrigen { get; set; }
}
