using EvolutionSender.Models;

namespace EvolutionSender.Data;

public interface IMembroRepository
{
    Task<IReadOnlyList<Membro>> ListarAsync(string? busca, bool incluirInativos);
    Task<Membro?> ObterPorCodigoAsync(int codigo);
    Task<int> CriarAsync(Membro membro);
    Task<bool> AtualizarAsync(Membro membro);
    Task<bool> ExcluirAsync(int codigo);
}
