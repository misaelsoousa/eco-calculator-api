using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class Servicos
{
    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("porto")]
    public string Porto { get; set; } = string.Empty;

    [JsonPropertyName("ordem")]
    public int Ordem { get; set; } 
    [JsonPropertyName("consumo")]
    public float Consumo { get; set; }
}
