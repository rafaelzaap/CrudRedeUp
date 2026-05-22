namespace EvolutionSender.Models;

public class AniversarioEnvioHistorico
{
    public long Id { get; set; }
    public int MembroCodigo { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateTime DataReferencia { get; set; }
    public string TelefoneDestino { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Erro { get; set; }
    public DateTime CriadoEm { get; set; }
}
