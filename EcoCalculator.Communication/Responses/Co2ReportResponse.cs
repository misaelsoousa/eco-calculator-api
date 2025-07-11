using System.Collections.Generic;

namespace EcoCalculator.Communication.Responses
{
    public class Co2ReportResponse
    {
        public double EmissaoRodoviario { get; set; }
        public double EmissaoMaritimo { get; set; }
        public double EconomiaCo2 { get; set; }
        public Equivalencias Equivalencias { get; set; } = new Equivalencias();
        public List<EtapaTransporte> Etapas { get; set; }  = new List<EtapaTransporte>();
    }

    public class Equivalencias
    {
        public int ArvoresPlantadas { get; set; }
        public double GeloArctico { get; set; }
        public int CaminhoesRetirados { get; set; }
        public int CreditosCarbono { get; set; }
    }

    public class EtapaTransporte
    {
        public string Origem { get; set; } = string.Empty;
        public string Destino { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public double EmissaoCo2 { get; set; }
    }
} 