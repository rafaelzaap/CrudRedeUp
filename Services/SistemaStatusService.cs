using Microsoft.Extensions.Options;
using MySqlConnector;

namespace EvolutionSender.Services;

public class SistemaStatusService : ISistemaStatusService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EvolutionApiOptions _evolutionOptions;
    private readonly ILogger<SistemaStatusService> _logger;

    public SistemaStatusService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IOptions<EvolutionApiOptions> evolutionOptions,
        ILogger<SistemaStatusService> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _evolutionOptions = evolutionOptions.Value;
        _logger = logger;
    }

    public async Task<SistemaStatus> VerificarAsync(CancellationToken cancellationToken = default)
    {
        var banco = await VerificarBancoAsync(cancellationToken);
        var evolution = await VerificarEvolutionAsync(cancellationToken);

        return new SistemaStatus
        {
            BancoOnline = banco.Online,
            EvolutionOnline = evolution.Online,
            ErroBanco = banco.Erro,
            ErroEvolution = evolution.Erro
        };
    }

    private async Task<(bool Online, string? Erro)> VerificarBancoAsync(CancellationToken cancellationToken)
    {
        var connectionString = _configuration.GetConnectionString("RedeUp");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return (false, "Connection string RedeUp nao configurada.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync(timeout.Token);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            command.CommandTimeout = 2;
            await command.ExecuteScalarAsync(timeout.Token);

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Banco indisponivel na verificacao de status.");
            return (false, "Banco de dados indisponivel. Confira se o container do MariaDB esta ligado.");
        }
    }

    private async Task<(bool Online, string? Erro)> VerificarEvolutionAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_evolutionOptions.BaseUrl))
        {
            return (false, "BaseUrl da Evolution API nao configurada.");
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            var client = _httpClientFactory.CreateClient("EvolutionHealth");
            using var request = new HttpRequestMessage(HttpMethod.Get, _evolutionOptions.BaseUrl);
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);

            return ((int)response.StatusCode < 500, null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Evolution API indisponivel na verificacao de status.");
            return (false, "Evolution API indisponivel. Confira se o container da Evolution esta ligado.");
        }
    }
}
