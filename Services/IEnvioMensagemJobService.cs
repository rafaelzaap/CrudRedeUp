namespace EvolutionSender.Services;

public interface IEnvioMensagemJobService
{
    EnvioMensagemJob Enfileirar(int? mensagemId);
    IReadOnlyList<EnvioMensagemJob> ListarRecentes(int quantidade = 10, int? mensagemId = null);
}
