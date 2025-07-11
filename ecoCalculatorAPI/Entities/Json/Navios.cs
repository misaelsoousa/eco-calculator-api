using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class Navios
{
    [JsonPropertyName("nos")]
    public float Nos { get; set; }

    [JsonPropertyName("consumo")]
    public float Consumo { get; set; }
    [JsonPropertyName("rpm")]
    public float Rpm { get; set; }
}