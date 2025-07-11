using EcoCalculator.Communication.Requests;
using EcoCalculator.Communication.Responses;
using ecoCalculatorAPI.Entities;
using System.Text.Json;

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
        var rotas = MemoryData.Data.OrigemDestino;
        var municipios = MemoryData.Data.Municipios;

        var nomeCidadeOrigem = ExtrairCidade(request.Origem);
        var nomeCidadeDestino = ExtrairCidade(request.Destino);

        var municipioOrigem = municipios.FirstOrDefault(m => m.Cidade.Equals(nomeCidadeOrigem, StringComparison.OrdinalIgnoreCase));
        var municipioDestino = municipios.FirstOrDefault(m => m.Cidade.Equals(nomeCidadeDestino, StringComparison.OrdinalIgnoreCase));

        if (municipioOrigem == null || municipioDestino == null)
            throw new Exception("Cidade de origem ou destino não encontrada no banco de municípios.");

        var portoOrigem = municipioOrigem.PortoProximo;
        var portoDestino = municipioDestino.PortoProximo;

        string origemCompleta = $"{municipioOrigem.Cidade}, {municipioOrigem.Estado}";
        string destinoCompleta = $"{municipioDestino.Cidade}, {municipioDestino.Estado}";

        var portos = MemoryData.Data.ConsumoPortos;
        var portoOrigemObj = portos.FirstOrDefault(p => p.Code == portoOrigem);
        var portoDestinoObj = portos.FirstOrDefault(p => p.Code == portoDestino);
        var portoOrigemNome = portoOrigemObj?.Port ?? portoOrigem;
        var portoDestinoNome = portoDestinoObj?.Port ?? portoDestino;

        var servicos = MemoryData.Data.Servicos;
        float consumoPortoOrigem = servicos.FirstOrDefault(s => s.Porto == portoOrigem)?.Consumo ?? 0.002f;
        float consumoPortoDestino = servicos.FirstOrDefault(s => s.Porto == portoDestino)?.Consumo ?? 0.002f;

        double tempoPortoOrigem = 0;
        double tempoPortoDestino = 0;
        double.TryParse(portoOrigemObj?.AverageH, System.Globalization.CultureInfo.InvariantCulture, out tempoPortoOrigem);
        double.TryParse(portoDestinoObj?.AverageH, System.Globalization.CultureInfo.InvariantCulture, out tempoPortoDestino);

        double tonFfeKmTruck = double.Parse(variaveis.First(v => v.Descricao == "ton_ffe_km_truck").Valor.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
        double tonFfeKmDry = double.Parse(variaveis.First(v => v.Descricao == "ton_ffe_km_dry").Valor.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
        double tonFfeKmReefer = double.Parse(variaveis.First(v => v.Descricao == "ton_ffe_km_reefer").Valor.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);
        double conversaoTonTeu = double.Parse(variaveis.First(v => v.Descricao == "conversao_ton_teu").Valor.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture);

        double fatorMaritimo = request.Formato == EcoCalculator.Communication.Enums.ContainerFormat.Reefer ? tonFfeKmReefer : tonFfeKmDry;

        double quantidadeTon = request.Quantidade > 0 ? request.Quantidade : 1;
        if (request.Carregamento == EcoCalculator.Communication.Enums.ContainerLoad.TEU)
        {
            if (request.TipoContainer == EcoCalculator.Communication.Enums.ContainerType.Forty)
                quantidadeTon = quantidadeTon * 2 * conversaoTonTeu;
            else
                quantidadeTon = quantidadeTon * conversaoTonTeu;
        }

        double distanciaOrigemPorto = await BuscarDistanciaGoogle(origemCompleta, portoOrigemNome);
        double distanciaPortoPorto = await BuscarDistanciaGoogle(portoOrigemNome, portoDestinoNome);
        double distanciaPortoDestino = await BuscarDistanciaGoogle(portoDestinoNome, destinoCompleta);

        double emissaoAtividadePortoOrigem = Math.Round(quantidadeTon * consumoPortoOrigem * tempoPortoOrigem, 4);
        double emissaoAtividadePortoDestino = Math.Round(quantidadeTon * consumoPortoDestino * tempoPortoDestino, 4);

        double emissaoCabotagem = Math.Round(distanciaPortoPorto * fatorMaritimo * quantidadeTon, 4);

        double emissaoRodoviarioOrigem = Math.Round(distanciaOrigemPorto * tonFfeKmTruck * quantidadeTon, 4);
        double emissaoRodoviarioDestino = Math.Round(distanciaPortoDestino * tonFfeKmTruck * quantidadeTon, 4);
        double emissaoRodoviario = emissaoRodoviarioOrigem + emissaoRodoviarioDestino;
        double emissaoTotal = emissaoRodoviario + emissaoAtividadePortoOrigem + emissaoCabotagem + emissaoAtividadePortoDestino;
        double emissaoRodoviarioPuro = Math.Round((distanciaOrigemPorto + distanciaPortoPorto + distanciaPortoDestino) * tonFfeKmTruck * quantidadeTon, 4);
        double economiaCo2 = emissaoRodoviarioPuro - emissaoTotal;
        if (economiaCo2 < 0) economiaCo2 = 0;

        int arvores = (int)Math.Round(economiaCo2 * 16.5);
        double gelo = Math.Round(economiaCo2 * 3, 2);
        int caminhoes = (int)Math.Round(economiaCo2 / 3.5);
        int creditos = (int)Math.Round(economiaCo2 / 1.2);

        var etapas = new List<EcoCalculator.Communication.Responses.EtapaTransporte>
        {
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Origem = $"{municipioOrigem.Cidade}, {municipioOrigem.Estado}",
                Destino = portoOrigemNome,
                Tipo = "Rodoviário",
                EmissaoCo2 = Math.Round(emissaoRodoviarioOrigem, 2)
            },
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Tipo = "Atividade Portuária",
                  EmissaoCo2 = Math.Round(emissaoAtividadePortoOrigem + emissaoAtividadePortoDestino, 2)
            },
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Origem = portoOrigemNome,
                Destino = portoDestinoNome,
                Tipo = "Cabotagem",
               EmissaoCo2 = Math.Round(emissaoCabotagem, 2)
            },
            new EcoCalculator.Communication.Responses.EtapaTransporte
            {
                Origem = portoDestinoNome,
                Destino = $"{municipioDestino.Cidade}, {municipioDestino.Estado}",
                Tipo = "Rodoviário",
                EmissaoCo2 = Math.Round(emissaoRodoviarioDestino, 2)
            }
        };

        var response = new EcoCalculator.Communication.Responses.Co2ReportResponse
        {
            EmissaoRodoviario = Math.Round(emissaoRodoviarioPuro, 2),
            EmissaoMaritimo = Math.Round(emissaoTotal, 2),
            EconomiaCo2 = Math.Round(economiaCo2, 2),
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
