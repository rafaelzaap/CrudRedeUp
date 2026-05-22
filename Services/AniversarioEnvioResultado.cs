namespace EvolutionSender.Services;

public class AniversarioEnvioResultado
{
    public int TotalEncontrados { get; init; }
    public int TotalEnviados { get; init; }
    public int TotalIgnorados { get; init; }
    public IReadOnlyList<string> Erros { get; init; } = [];
}
