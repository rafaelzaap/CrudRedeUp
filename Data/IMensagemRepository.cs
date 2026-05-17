using EvolutionSender.Models;

namespace EvolutionSender.Data;

public interface IMensagemRepository
{
    Task<IReadOnlyList<Mensagem>> ListarAsync(string? busca, bool incluirInativas);
    Task<IReadOnlyList<Mensagem>> ListarAtivasAsync();
    Task<Mensagem?> ObterAtivaAsync();
    Task<Mensagem?> ObterAtivaPorIdAsync(int id);
    Task<Mensagem?> ObterPorIdAsync(int id);
    Task<int> CriarAsync(Mensagem mensagem);
    Task<bool> AtualizarAsync(Mensagem mensagem);
    Task<bool> InativarAsync(int id);
    Task<bool> ExcluirAsync(int id);
}
