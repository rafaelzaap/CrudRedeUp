namespace EvolutionSender.Models;

public class EnvioMensagemHistorico
{
    public long Id { get; set; }
    public int MensagemId { get; set; }
    public int MembroCodigo { get; set; }
    public string MembroNome { get; set; } = string.Empty;
    public string TelefoneOriginal { get; set; } = string.Empty;
    public string TelefoneNormalizado { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Erro { get; set; }
    public DateTime CriadoEm { get; set; }
}
