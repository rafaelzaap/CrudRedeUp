using EvolutionSender.Models;

namespace EvolutionSender.Data;

public interface IEnvioMensagemHistoricoRepository
{
    Task RegistrarAsync(EnvioMensagemHistorico historico, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EnvioMensagemHistorico>> ListarRecentesAsync(
        int quantidade,
        int? mensagemId = null,
        CancellationToken cancellationToken = default);
}
