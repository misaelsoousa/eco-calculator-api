using EcoCalculator.Communication.Enums;

namespace EcoCalculator.Communication.Requests;

public class Co2Request
{
    public string Origem { get; set; } = string.Empty;
    public string Destino { get; set; } = string.Empty;
    public ContainerFormat Formato { get; set; }
    public ContainerType TipoContainer { get; set; }
    public double Quantidade { get; set; }
    public ContainerLoad Carregamento { get; set; }
}
