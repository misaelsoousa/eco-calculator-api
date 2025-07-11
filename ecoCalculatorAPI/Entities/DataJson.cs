using ecoCalculatorAPI.Entities.Json;
using System.Text.Json.Serialization;

namespace ecoCalculatorAPI.Entities;

public class DataJson
{
    public List<Municipio> Municipios { get; set; } = [];
    [JsonPropertyName("consumo_por_portos")]
    public List<ConsumoPortos> ConsumoPortos { get; set; } = [];
    [JsonPropertyName("navios")]
    public List<Navios> Navios { get; set; } = [];
    [JsonPropertyName("origem_destinos")]
    public List<OrigemDestino> OrigemDestino { get; set; } = [];
    [JsonPropertyName("rotas")]
    public List<Rotas> Rotas { get; set; } = [];
    [JsonPropertyName("servicos")]
    public List<Servicos> Servicos { get; set; } = [];
    [JsonPropertyName("transit_times")]
    public List<TransitTimes> TransitTimes { get; set; } = [];
    [JsonPropertyName("variaveis")]
    public List<Variaveis> Variaveis { get; set; } = [];
}
