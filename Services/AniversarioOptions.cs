namespace EvolutionSender.Services;

public class AniversarioOptions
{
    public string NumeroLembrete { get; set; } = "5522998918409";
    public string? GrupoLembreteJid { get; set; }
    public int MesesListagem { get; set; } = 3;
    public int HoraEnvioAutomatico { get; set; } = 9;
    public int MinutoEnvioAutomatico { get; set; } = 0;
    public string MensagemAniversariante { get; set; } =
        "Ola, {primeiro_nome}! A RedeUp deseja um feliz aniversario! Que Deus abencoe sua vida hoje e sempre.";
    public string MensagemLembrete { get; set; } =
        "Lembrete RedeUp: hoje e aniversario de {nome}. Telefone: {telefone}.";
}
