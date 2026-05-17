namespace EvolutionSender.Services;

public interface IEvolutionApiClient
{
    Task<EvolutionApiSendResult> EnviarTextoAsync(string numero, string texto, CancellationToken cancellationToken = default);
}
