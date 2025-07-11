using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class Municipio
{
    [JsonPropertyName("cidade")]
    public string Cidade { get; set; } = string.Empty;

    [JsonPropertyName("estado")]
    public string Estado { get; set; } = string.Empty;

    [JsonPropertyName("porto_proximo")]
    public string PortoProximo { get; set; } = string.Empty;

    [JsonPropertyName("lat")]
    public string Latitude { get; set; } = string.Empty;

    [JsonPropertyName("long")]
    public string Longitude { get; set; } = string.Empty;
}
