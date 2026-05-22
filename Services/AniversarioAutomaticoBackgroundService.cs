using Microsoft.Extensions.Options;

namespace EvolutionSender.Services;

public class AniversarioAutomaticoBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AniversarioAutomaticoBackgroundService> _logger;
    private readonly AniversarioOptions _options;
    private DateTime? _ultimaExecucao;

    public AniversarioAutomaticoBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<AniversarioAutomaticoBackgroundService> logger,
        IOptions<AniversarioOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerificarAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na rotina automatica de aniversarios.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }

    private async Task VerificarAsync(CancellationToken cancellationToken)
    {
        var agora = DateTime.Now;
        var horario = new TimeSpan(
            Math.Clamp(_options.HoraEnvioAutomatico, 0, 23),
            Math.Clamp(_options.MinutoEnvioAutomatico, 0, 59),
            0);

        if (agora.TimeOfDay < horario || _ultimaExecucao == agora.Date)
        {
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var aniversarioService = scope.ServiceProvider.GetRequiredService<IAniversarioService>();

        if (!await aniversarioService.ObterEnvioAutomaticoAtivoAsync(cancellationToken))
        {
            return;
        }

        var resultado = await aniversarioService.EnviarAutomaticoHojeAsync(cancellationToken);
        _ultimaExecucao = agora.Date;

        _logger.LogInformation(
            "Envio automatico de aniversarios executado. Encontrados: {Encontrados}. Enviados: {Enviados}. Ignorados: {Ignorados}. Erros: {Erros}.",
            resultado.TotalEncontrados,
            resultado.TotalEnviados,
            resultado.TotalIgnorados,
            resultado.Erros.Count);
    }
}
