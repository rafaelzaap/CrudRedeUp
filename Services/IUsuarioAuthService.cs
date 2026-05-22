using EvolutionSender.Models;

namespace EvolutionSender.Services;

public interface IUsuarioAuthService
{
    Task<bool> PrecisaConfigurarAdminAsync(CancellationToken cancellationToken = default);
    Task<(bool Sucesso, string? Erro, UsuarioSistema? Usuario)> EntrarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken = default);
    Task<(bool Sucesso, string? Erro)> CriarPrimeiroAdminAsync(
        string nome,
        string email,
        string senha,
        CancellationToken cancellationToken = default);
}
