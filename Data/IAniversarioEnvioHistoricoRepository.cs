using EvolutionSender.Models;

namespace EvolutionSender.Data;

public interface IAniversarioEnvioHistoricoRepository
{
    Task<bool> JaEnviadoAsync(
        int membroCodigo,
        string tipo,
        DateTime dataReferencia,
        CancellationToken cancellationToken = default);
    Task RegistrarAsync(AniversarioEnvioHistorico historico, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AniversarioEnvioHistorico>> ListarRecentesAsync(
        int quantidade = 20,
        CancellationToken cancellationToken = default);
}
