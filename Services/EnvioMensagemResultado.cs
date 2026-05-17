namespace EvolutionSender.Services;

public class EnvioMensagemResultado
{
    public int MensagemId { get; init; }
    public int TotalMensagens { get; init; }
    public int TotalMensagensInativadas { get; init; }
    public int TotalMembros { get; init; }
    public int TotalEnviados { get; init; }
    public int TotalErros { get; init; }
    public bool MensagemInativada { get; init; }
    public IReadOnlyList<string> Erros { get; init; } = [];

    public bool Sucesso => TotalEnviados > 0 && TotalErros == 0 && MensagemInativada;
}
