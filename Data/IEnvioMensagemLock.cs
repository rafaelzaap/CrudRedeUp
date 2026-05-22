namespace EvolutionSender.Data;

public interface IEnvioMensagemLock
{
    Task<IAsyncDisposable?> TentarAdquirirAsync(CancellationToken cancellationToken = default);
}
