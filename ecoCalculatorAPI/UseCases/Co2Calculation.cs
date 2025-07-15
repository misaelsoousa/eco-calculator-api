using EcoCalculator.Communication.Requests;
using EcoCalculator.Communication.Responses;
using ecoCalculatorAPI.Entities;
using System.Text.Json;
using System.Globalization;

namespace ecoCalculatorAPI.UseCases;

public class Co2Calculation
{
    public async Task LoadData()
    {
        var caminhoArquivo = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "Data.json");

        if (!File.Exists(caminhoArquivo))
            throw new FileNotFoundException($"Arquivo JSON não encontrado em {caminhoArquivo}");

        var json = await File.ReadAllTextAsync(caminhoArquivo);

        var data = JsonSerializer.Deserialize<DataJson>(json);

        if (data == null)
            throw new Exception("Erro ao desserializar o JSON");

        MemoryData.Data = data;
    }
    public async Task<List<CityResponse>> GetCities()
    {
        await LoadData();
        return MemoryData.Data.Municipios
            .Select(m => new CityResponse
            {
                Cidade = $"{m.Cidade} - {m.Estado}",
                Value = $"{m.Cidade},{m.Estado}"
            })
            .ToList();
    }
    private async Task<double> BuscarDistanciaGoogle(string origem, string destino)
    {
        string apiKey = "AIzaSyAuTVBt_737piBIr4ThhDvf7Cn1EWkT0o8";
        string url = $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={Uri.EscapeDataString(origem)}&destinations={Uri.EscapeDataString(destino)}&key={apiKey}&language=pt";

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                throw new Exception("Erro ao consultar a API do Google Distance Matrix");

            var json = await response.Content.ReadAsStringAsync();
            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                var root = doc.RootElement;
                var rows = root.GetProperty("rows");
                if (rows.GetArrayLength() > 0)
                {
                    var elements = rows[0].GetProperty("elements");
                    if (elements.GetArrayLength() > 0)
                    {
                        var distance = elements[0].GetProperty("distance");
                        var value = distance.GetProperty("value").GetDouble(); 
                        return value / 1000.0; 
                    }
                }
            }
        }
        throw new Exception("Distância não encontrada na resposta da API do Google");
    }

    private string ExtrairCidade(string input)
    {
        if (input.Contains(","))
            return input.Split(',')[0].Trim();
        return input.Trim();
    }


    public async Task<Dictionary<string, object>> CalcularEmissoes(Co2Request request)
    {
        await LoadData();
        var variaveis = MemoryData.Data.Variaveis;
        var municipios = MemoryData.Data.Municipios;
        var portos = MemoryData.Data.ConsumoPortos;
        var servicos = MemoryData.Data.Servicos;
        var rotas = MemoryData.Data.OrigemDestino;

        var nomeCidadeOrigem = ExtrairCidade(request.Origem);
        var nomeCidadeDestino = ExtrairCidade(request.Destino);
        var municipioOrigem = municipios.FirstOrDefault(m => m.Cidade.Equals(nomeCidadeOrigem, StringComparison.OrdinalIgnoreCase));
        var municipioDestino = municipios.FirstOrDefault(m => m.Cidade.Equals(nomeCidadeDestino, StringComparison.OrdinalIgnoreCase));
        if (municipioOrigem == null || municipioDestino == null)
            throw new Exception("Cidade de origem ou destino não encontrada no banco de municípios.");

        var portoOrigem = municipioOrigem.PortoProximo;
        var portoDestino = municipioDestino.PortoProximo;
        var portoOrigemObj = portos.FirstOrDefault(p => p.Code == portoOrigem);
        var portoDestinoObj = portos.FirstOrDefault(p => p.Code == portoDestino);
        var portoOrigemNome = portoOrigemObj?.Port ?? portoOrigem;
        var portoDestinoNome = portoDestinoObj?.Port ?? portoDestino;

        string origemCompleta = $"{municipioOrigem.Cidade}, {municipioOrigem.Estado}";
        string destinoCompleta = $"{municipioDestino.Cidade}, {municipioDestino.Estado}";

        double quantidade = request.Quantidade > 0 ? request.Quantidade : 1;
        double conteiner = 0;
        if (request.Carregamento == EcoCalculator.Communication.Enums.ContainerLoad.TEU)
        {
            if (request.TipoContainer == EcoCalculator.Communication.Enums.ContainerType.Forty)
                conteiner = Math.Ceiling(quantidade / 10.0); 
            else
                conteiner = quantidade;
            conteiner = ((conteiner + 0.5) * 2.5) / 6.32996;
            conteiner = Math.Ceiling(conteiner);
        }
        else
        {
            conteiner = Math.Ceiling(quantidade / 10.0);
            conteiner = ((conteiner + 0.5) * 2.5) / 6.32996;
            conteiner = Math.Ceiling(conteiner);
        }

        // Fatores do data.json
        double consumoRodoviario = double.Parse(variaveis.First(v => v.Descricao == "consumo").Valor.Replace(',', '.'), CultureInfo.InvariantCulture);
        double lCombKgCo2 = double.Parse(variaveis.First(v => v.Descricao == "combustivel_por_kgco2").Valor.Replace(',', '.'), CultureInfo.InvariantCulture);
        double fatorRodoviario = consumoRodoviario * lCombKgCo2 / 1000.0;
        double fatorDry = double.Parse(variaveis.First(v => v.Descricao == "conversao_combustivel_co2_dry").Valor.Replace(',', '.'), CultureInfo.InvariantCulture);
        double fatorReefer = double.Parse(variaveis.First(v => v.Descricao == "conversao_combustivel_co2_reefer").Valor.Replace(',', '.'), CultureInfo.InvariantCulture);
        double fatorMaritimo = request.Formato == EcoCalculator.Communication.Enums.ContainerFormat.Reefer ? fatorReefer : fatorDry;

        double distanciaOrigemPorto = await BuscarDistanciaGoogle(origemCompleta, portoOrigemNome);
        double distanciaPortoDestino = await BuscarDistanciaGoogle(portoDestinoNome, destinoCompleta);
        double distanciaPortoPorto = await BuscarDistanciaGoogle(portoOrigemNome, portoDestinoNome);

        float consumoPortoOrigem = servicos.FirstOrDefault(s => s.Porto == portoOrigem)?.Consumo ?? 0.002f;
        float consumoPortoDestino = servicos.FirstOrDefault(s => s.Porto == portoDestino)?.Consumo ?? 0.002f;
        double tempoPortoOrigem = 0, tempoPortoDestino = 0;
        double.TryParse(portoOrigemObj?.AverageH, System.Globalization.CultureInfo.InvariantCulture, out tempoPortoOrigem);
        double.TryParse(portoDestinoObj?.AverageH, System.Globalization.CultureInfo.InvariantCulture, out tempoPortoDestino);
        double relAtivPorto = (consumoPortoOrigem * tempoPortoOrigem + consumoPortoDestino * tempoPortoDestino) * conteiner * fatorMaritimo;

        var navios = MemoryData.Data.Navios;
        double velocidadeNos = double.Parse(variaveis.First(v => v.Descricao == "velocidade_nos").Valor.Replace(',', '.'), CultureInfo.InvariantCulture);
        double capacidadeTeus = double.Parse(variaveis.First(v => v.Descricao == "capacidade_teus").Valor.Replace(',', '.'), CultureInfo.InvariantCulture);
        var navioMaisProximo = navios.OrderBy(n => Math.Abs(n.Nos - velocidadeNos)).First();
        double consumoPorNos = navioMaisProximo.Consumo;
        double velocidadeKmDia = velocidadeNos * 44.448;
        double consumoPorTeuKm = consumoPorNos / (capacidadeTeus * velocidadeKmDia);
        double relCabotagem = distanciaPortoPorto * conteiner * consumoPorTeuKm * fatorMaritimo;

        double relRodoviario = (distanciaOrigemPorto + distanciaPortoPorto + distanciaPortoDestino) * conteiner * fatorRodoviario;

        double relPortaPorto = distanciaOrigemPorto * conteiner * fatorRodoviario;
        double relPortoPorta = distanciaPortoDestino * conteiner * fatorRodoviario;

        double totalMercosul = relPortaPorto + relAtivPorto + relCabotagem + relPortoPorta;
        double economia = Math.Round(relRodoviario - totalMercosul, 2);
        if (economia < 0) economia = 0;
        int arvores = (int)Math.Round(economia / 0.060493, 0);
        double gelo = Math.Round(economia * 3, 1);
        int caminhoes = (int)Math.Ceiling(2.5 * (Math.Ceiling(quantidade) + 0.5) / 6.32996);
        int creditos = (int)Math.Floor(economia);

        var etapas = new List<EcoCalculator.Communication.Responses.EtapaTransporte>
        {
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Origem = $"{municipioOrigem.Cidade}, {municipioOrigem.Estado}",
                Destino = portoOrigemNome,
                Tipo = "Rodoviário",
                EmissaoCo2 = Math.Round(relPortaPorto, 2)
            },
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Tipo = "Atividade Portuária",
                EmissaoCo2 = Math.Round(relAtivPorto, 2)
            },
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Origem = portoOrigemNome,
                Destino = portoDestinoNome,
                Tipo = "Cabotagem",
                EmissaoCo2 = Math.Round(relCabotagem, 2)
            },
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Origem = portoDestinoNome,
                Destino = $"{municipioDestino.Cidade}, {municipioDestino.Estado}",
                Tipo = "Rodoviário",
                EmissaoCo2 = Math.Round(relPortoPorta, 2)
            }
        };

        var response = new EcoCalculator.Communication.Responses.Co2ReportResponse
        {
            EmissaoRodoviario = Math.Round(relRodoviario, 2),
            EmissaoMaritimo = Math.Round(totalMercosul, 2),
            EconomiaCo2 = Math.Round(economia, 2),
            Equivalencias = new EcoCalculator.Communication.Responses.Equivalencias
            {
                ArvoresPlantadas = arvores,
                GeloArctico = gelo,
                CaminhoesRetirados = caminhoes,
                CreditosCarbono = creditos
            },
            Etapas = etapas
        };

        string servicoMaisRapido = "Indisponível";
        var rotasMaisRapidas = MemoryData.Data.Rotas;
        var rotaMaisRapida = rotasMaisRapidas.FirstOrDefault(r => r.Origem == portoOrigem && r.Destino == portoDestino);
        if (rotaMaisRapida != null && !string.IsNullOrEmpty(rotaMaisRapida.Rota))
        {
            servicoMaisRapido = rotaMaisRapida.Rota;
        }
        string carregamentoContainer = $"{request.Quantidade} {(request.Carregamento == EcoCalculator.Communication.Enums.ContainerLoad.TEU ? "TEU" : "TON" )}";
        return new Dictionary<string, object> { { "relatorio", response }, { "servicoMaisRapido", servicoMaisRapido }, { "carregamentoContainer", carregamentoContainer } };
    }

   
}
