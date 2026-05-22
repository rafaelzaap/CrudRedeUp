namespace EvolutionSender.Data;

public interface ISistemaConfiguracaoRepository
{
    Task<string?> ObterAsync(string chave, CancellationToken cancellationToken = default);
    Task SalvarAsync(string chave, string valor, CancellationToken cancellationToken = default);
}
