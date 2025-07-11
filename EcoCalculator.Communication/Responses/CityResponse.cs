using System.Text.Json.Serialization;

namespace EcoCalculator.Communication.Responses;

public class CityResponse
{
    public string Cidade { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

}
