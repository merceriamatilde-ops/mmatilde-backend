using System.Text.Json.Serialization;

namespace MMatilde.Api.Models;

public class ProductoRelacionado
{
    public int ProductoPrincipalId { get; set; }
    
    [JsonIgnore]
    public Producto ProductoPrincipal { get; set; } = null!;

    public int ProductoVinculadoId { get; set; }
    
    [JsonIgnore]
    public Producto ProductoVinculado { get; set; } = null!;
}
