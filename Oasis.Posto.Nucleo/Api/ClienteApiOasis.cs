using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Oasis.Posto.Dominio;

namespace Oasis.Posto.Api;

public sealed record RiscoEstacao(
    string Estacao, string Nome, double? Temperatura, double? Umidade, double? IndiceCalor, NivelRisco? Nivel, DateTimeOffset? RecebidaEm, bool Atualizada);

public sealed record Previsao(DateOnly DataAlvo, double Probabilidade, bool Risco, double Limiar, string VersaoModelo, int LeiturasUsadas);

public sealed record AlertaApi(
    int Id, string? Estacao, NivelRisco Nivel, string Origem, string Mensagem, DateOnly? DataAlvo, DateTimeOffset Inicio, DateTimeOffset? Fim);

public sealed record PontoApoio(
    int Id, string Nome, string Tipo, string Endereco, string Bairro, double Latitude, double Longitude, string? Horario, string Status, DateTimeOffset CriadoEm);

internal sealed record Problema(string? Title, int Status, string? Detail);

public sealed class ErroApi(string mensagem, HttpStatusCode? status = null) : Exception(mensagem)
{
    public HttpStatusCode? Status { get; } = status;
}

/// <summary>Cliente da API Oásis usado pelo Painel do Posto (HttpClient + System.Text.Json).</summary>
public sealed class ClienteApiOasis : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly HttpClient _http;

    public ClienteApiOasis(string urlBase, string chavePosto, HttpMessageHandler? handler = null)
    {
        _http = handler is null ? new HttpClient() : new HttpClient(handler);
        _http.BaseAddress = new Uri(urlBase.TrimEnd('/') + "/");
        _http.Timeout = TimeSpan.FromSeconds(10);
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        if (!string.IsNullOrWhiteSpace(chavePosto))
            _http.DefaultRequestHeaders.Add("X-Chave-Posto", chavePosto);
    }

    public async Task<bool> EstaNoArAsync(CancellationToken ct = default)
    {
        try
        {
            using var resposta = await _http.GetAsync("health", ct);
            return resposta.IsSuccessStatusCode;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            return false;
        }
    }

    public Task<List<RiscoEstacao>> RiscoAgoraAsync(CancellationToken ct = default) => ObterAsync<List<RiscoEstacao>>("api/risco/agora", ct);

    public Task<List<AlertaApi>> AlertasAtivosAsync(CancellationToken ct = default) => ObterAsync<List<AlertaApi>>("api/alertas?ativos=true", ct);

    public Task<List<PontoApoio>> PontosPendentesAsync(CancellationToken ct = default) => ObterAsync<List<PontoApoio>>("api/pontos?status=Pendente", ct);

    /// <summary>Previsão de amanhã. Devolve null se ainda não há leituras hoje (a API responde 422).</summary>
    public async Task<Previsao?> PrevisaoAmanhaAsync(CancellationToken ct = default)
    {
        try
        {
            return await ObterAsync<Previsao>("api/previsoes/amanha", ct);
        }
        catch (ErroApi erro) when (erro.Status == HttpStatusCode.UnprocessableEntity)
        {
            return null;
        }
    }

    public async Task<PontoApoio> AlterarStatusPontoAsync(int id, string status, CancellationToken ct = default)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Patch, $"api/pontos/{id}/status")
        {
            Content = JsonContent.Create(new { status }, options: Json),
        };
        return await EnviarAsync<PontoApoio>(pedido, ct);
    }

    private async Task<T> ObterAsync<T>(string caminho, CancellationToken ct)
    {
        using var pedido = new HttpRequestMessage(HttpMethod.Get, caminho);
        return await EnviarAsync<T>(pedido, ct);
    }

    private async Task<T> EnviarAsync<T>(HttpRequestMessage pedido, CancellationToken ct)
    {
        HttpResponseMessage resposta;
        try
        {
            resposta = await _http.SendAsync(pedido, ct);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            throw new ErroApi("Sem conexão com a API Oásis. Confira o endereço em Configurações e a rede.");
        }

        using (resposta)
        {
            if (!resposta.IsSuccessStatusCode)
            {
                var problema = await LerProblemaAsync(resposta, ct);
                var mensagem = resposta.StatusCode == HttpStatusCode.Unauthorized
                    ? "A API recusou a chave do posto. Confira em Configurações."
                    : problema?.Detail ?? $"A API respondeu com erro {(int)resposta.StatusCode}.";
                throw new ErroApi(mensagem, resposta.StatusCode);
            }
            return await resposta.Content.ReadFromJsonAsync<T>(Json, ct)
                ?? throw new ErroApi("A API mandou uma resposta vazia.", resposta.StatusCode);
        }
    }

    private static async Task<Problema?> LerProblemaAsync(HttpResponseMessage resposta, CancellationToken ct)
    {
        try
        {
            return await resposta.Content.ReadFromJsonAsync<Problema>(Json, ct);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public void Dispose() => _http.Dispose();
}
