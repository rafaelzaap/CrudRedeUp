namespace EvolutionSender.Services;

public interface ISistemaStatusService
{
    Task<SistemaStatus> VerificarAsync(CancellationToken cancellationToken = default);
}
