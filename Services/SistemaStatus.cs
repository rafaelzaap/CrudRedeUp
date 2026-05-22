namespace EvolutionSender.Services;

public class SistemaStatus
{
    public bool BancoOnline { get; init; }
    public bool EvolutionOnline { get; init; }
    public string? ErroBanco { get; init; }
    public string? ErroEvolution { get; init; }

    public bool TudoOnline => BancoOnline && EvolutionOnline;
}
