using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities.Json;

public class Variaveis
{
    [JsonPropertyName("descricao")]
    public string Descricao { get; set; } = string.Empty;

    [JsonPropertyName("nome")]
    public string Nome { get; set; } = string.Empty;

    [JsonPropertyName("valor")]
    public string Valor { get; set; } = string.Empty;

}
