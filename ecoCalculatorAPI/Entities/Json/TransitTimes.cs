using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class TransitTimes
{
    [JsonPropertyName("origem")]
    public string Origem { get; set; } = string.Empty;

    [JsonPropertyName("destino")]
    public string Destino { get; set; } = string.Empty;

    [JsonPropertyName("servico")]
    public string Servico { get; set; } = string.Empty;
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = string.Empty;
}
