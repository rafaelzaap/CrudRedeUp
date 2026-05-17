namespace EvolutionSender.Services;

public interface IEnvioMensagemService
{
    Task<EnvioMensagemResultado> EnviarMensagemAtivaAsync(CancellationToken cancellationToken = default);
    Task<EnvioMensagemResultado> EnviarTodasMensagensAtivasAsync(CancellationToken cancellationToken = default);
    Task<EnvioMensagemResultado> EnviarMensagemAtivaAsync(int mensagemId, CancellationToken cancellationToken = default);
}
