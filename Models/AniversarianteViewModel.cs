namespace EvolutionSender.Models;

public class AniversarianteViewModel
{
    public int Codigo { get; init; }
    public string Nome { get; init; } = string.Empty;
    public string PrimeiroNome { get; init; } = string.Empty;
    public string? Telefone { get; init; }
    public DateTime DataDeNascimento { get; init; }
    public DateTime ProximoAniversario { get; init; }
    public int DiasRestantes { get; init; }
    public int IdadeNova { get; init; }
}
