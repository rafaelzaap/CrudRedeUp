using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace EvolutionSender.Services;

public class EvolutionApiClient : IEvolutionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly EvolutionApiOptions _options;

    public EvolutionApiClient(HttpClient httpClient, IOptions<EvolutionApiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<EvolutionApiSendResult> EnviarTextoAsync(
        string numero,
        string texto,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.BaseUrl)
            || string.IsNullOrWhiteSpace(_options.InstanceName)
            || string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            return new EvolutionApiSendResult
            {
                Sucesso = false,
                Erro = "Configure BaseUrl, InstanceName e ApiKey da Evolution API. Use appsettings.Local.json, user-secrets ou variaveis de ambiente."
            };
        }

        var endpoint = _options.SendTextEndpointTemplate
            .Replace("{instance}", Uri.EscapeDataString(_options.InstanceName));

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.TryAddWithoutValidation(_options.ApiKeyHeaderName, _options.ApiKey);

        request.Content = JsonContent.Create(new
        {
            number = numero,
            text = texto
        });

        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return new EvolutionApiSendResult { Sucesso = true };
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        return new EvolutionApiSendResult
        {
            Sucesso = false,
            Erro = $"Evolution API retornou {(int)response.StatusCode}: {body}"
        };
    }
}
