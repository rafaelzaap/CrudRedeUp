namespace EvolutionSender.Services;

public interface IEnvioMensagemService
{
    Task<EnvioMensagemResultado> EnviarMensagemAtivaAsync(
        CancellationToken cancellationToken = default,
        IProgress<EnvioMensagemProgresso>? progresso = null);
    Task<EnvioMensagemResultado> EnviarTodasMensagensAtivasAsync(
        CancellationToken cancellationToken = default,
        IProgress<EnvioMensagemProgresso>? progresso = null);
    Task<EnvioMensagemResultado> EnviarMensagemAtivaAsync(
        int mensagemId,
        CancellationToken cancellationToken = default,
        IProgress<EnvioMensagemProgresso>? progresso = null);
}
