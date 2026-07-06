using System.Text.Json.Serialization;

namespace MMatilde.Api.Models;

public class ProductoTag
{
    public int ProductoId { get; set; }

    [JsonIgnore]
    public Producto Producto { get; set; } = null!;

    public int TagId { get; set; }

    [JsonIgnore]
    public Tag Tag { get; set; } = null!;
}
