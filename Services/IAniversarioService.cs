using EvolutionSender.Models;

namespace EvolutionSender.Services;

public interface IAniversarioService
{
    Task<IReadOnlyList<AniversarianteViewModel>> ListarProximosAsync(
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AniversarioEnvioHistorico>> ListarHistoricoAsync(
        CancellationToken cancellationToken = default);
    Task<bool> ObterEnvioAutomaticoAtivoAsync(CancellationToken cancellationToken = default);
    Task DefinirEnvioAutomaticoAtivoAsync(bool ativo, CancellationToken cancellationToken = default);
    Task<bool> ExisteAniversarianteHojeAsync(CancellationToken cancellationToken = default);
    Task<AniversarioEnvioResultado> EnviarMensagemParaMembroAsync(
        int membroCodigo,
        CancellationToken cancellationToken = default);
    Task<AniversarioEnvioResultado> EnviarLembreteAsync(
        int membroCodigo,
        CancellationToken cancellationToken = default);
    Task<AniversarioEnvioResultado> EnviarAutomaticoHojeAsync(
        CancellationToken cancellationToken = default);
}
