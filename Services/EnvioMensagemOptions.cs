namespace EvolutionSender.Services;

public class EnvioMensagemOptions
{
    public int DelayMinimoSegundos { get; set; } = 5;
    public int DelayMaximoSegundos { get; set; } = 10;
    public bool AdicionarSaudacao { get; set; } = true;
    public string SaudacaoTemplate { get; set; } = "Oi, {primeiro_nome}. Tudo bem?";
}
