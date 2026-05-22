namespace EvolutionSender.Services;

public class EnvioMensagemJob
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int? MensagemId { get; init; }
    public string Status { get; set; } = "Pendente";
    public DateTime CriadoEm { get; init; } = DateTime.Now;
    public DateTime? IniciadoEm { get; set; }
    public DateTime? FinalizadoEm { get; set; }
    public int TotalItens { get; set; }
    public int ItensProcessados { get; set; }
    public string? ItemAtual { get; set; }
    public EnvioMensagemResultado? Resultado { get; set; }
    public string? Erro { get; set; }

    public string Descricao => MensagemId.HasValue
        ? $"Mensagem {MensagemId.Value}"
        : "Todas as mensagens ativas";

    public int Percentual
    {
        get
        {
            if (TotalItens <= 0)
            {
                return Status == "Concluido" ? 100 : 0;
            }

            return Math.Clamp((int)Math.Round(ItensProcessados * 100d / TotalItens), 0, 100);
        }
    }
}
