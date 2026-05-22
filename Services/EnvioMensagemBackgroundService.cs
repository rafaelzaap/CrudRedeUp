namespace EvolutionSender.Services;

public class EnvioMensagemBackgroundService : BackgroundService
{
    private readonly IEnvioMensagemJobQueue _queue;
    private readonly EnvioMensagemJobService _jobService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EnvioMensagemBackgroundService> _logger;

    public EnvioMensagemBackgroundService(
        IEnvioMensagemJobQueue queue,
        EnvioMensagemJobService jobService,
        IServiceScopeFactory scopeFactory,
        ILogger<EnvioMensagemBackgroundService> logger)
    {
        _queue = queue;
        _jobService = jobService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var jobId = await _queue.AguardarProximoAsync(stoppingToken);

            if (!_jobService.TentarObter(jobId, out var job) || job is null)
            {
                continue;
            }

            job.Status = "Processando";
            job.IniciadoEm = DateTime.Now;

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var envioService = scope.ServiceProvider.GetRequiredService<IEnvioMensagemService>();
                var progresso = new Progress<EnvioMensagemProgresso>(atual =>
                {
                    job.TotalItens = atual.TotalItens;
                    job.ItensProcessados = atual.ItensProcessados;
                    job.ItemAtual = atual.ItemAtual;
                });

                job.Resultado = job.MensagemId.HasValue
                    ? await envioService.EnviarMensagemAtivaAsync(job.MensagemId.Value, stoppingToken, progresso)
                    : await envioService.EnviarTodasMensagensAtivasAsync(stoppingToken, progresso);

                job.Status = job.Resultado.TotalErros > 0 ? "Concluido com erros" : "Concluido";
                job.TotalItens = Math.Max(job.TotalItens, job.ItensProcessados);
                job.ItensProcessados = job.TotalItens;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                job.Status = "Cancelado";
                throw;
            }
            catch (Exception ex)
            {
                job.Status = "Falhou";
                job.Erro = ex.Message;
                _logger.LogError(ex, "Falha ao processar envio em segundo plano. JobId: {JobId}", job.Id);
            }
            finally
            {
                job.FinalizadoEm = DateTime.Now;
            }
        }
    }
}
