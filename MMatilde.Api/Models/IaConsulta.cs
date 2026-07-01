namespace MMatilde.Api.Models;

public class IaConsulta
{
    public int Id { get; set; }
    public string Proyecto { get; set; } = "";
    public string? Tecnica { get; set; }
    public string ContextoJson { get; set; } = "{}";
    public string ResultadoJson { get; set; } = "{}";
    public string? ProductosJson { get; set; }
    public string? Evaluacion { get; set; }
    public string? NotaCorreccion { get; set; }
    public string? CorreccionEsperada { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreadoEn { get; set; } = DateTime.UtcNow;
    public DateTime? RevisadoEn { get; set; }
}
