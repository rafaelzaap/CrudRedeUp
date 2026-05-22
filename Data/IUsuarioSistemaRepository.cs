using EvolutionSender.Models;

namespace EvolutionSender.Data;

public interface IUsuarioSistemaRepository
{
    Task<bool> ExisteUsuarioAsync(CancellationToken cancellationToken = default);
    Task<UsuarioSistema?> ObterPorEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<int> CriarAsync(UsuarioSistema usuario, CancellationToken cancellationToken = default);
    Task RegistrarLoginSucessoAsync(int id, CancellationToken cancellationToken = default);
    Task RegistrarFalhaLoginAsync(int id, DateTime? bloquearAte, CancellationToken cancellationToken = default);
}
