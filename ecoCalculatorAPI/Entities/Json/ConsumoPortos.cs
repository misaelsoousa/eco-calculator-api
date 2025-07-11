using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class ConsumoPortos
{
    [JsonPropertyName("port")]
    public string Port { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("average_stay_hms")]
    public string AverageHMS { get; set; } = string.Empty;
    [JsonPropertyName("average_stay_h")]
    public string AverageH { get; set; } = string.Empty;

}
