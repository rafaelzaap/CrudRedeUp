namespace EvolutionSender.Services;

public interface IEnvioMensagemJobQueue
{
    Task EnfileirarAsync(Guid jobId, CancellationToken cancellationToken = default);
    ValueTask<Guid> AguardarProximoAsync(CancellationToken cancellationToken = default);
}
