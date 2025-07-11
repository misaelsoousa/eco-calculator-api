using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class Rotas
{
    [JsonPropertyName("origem")]
    public string Origem { get; set; } = string.Empty;

    [JsonPropertyName("destino")]
    public string Destino { get; set; } = string.Empty;

    [JsonPropertyName("rota")]
    public string Rota { get; set; } = string.Empty;
}
