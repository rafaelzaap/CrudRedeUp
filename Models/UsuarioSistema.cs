namespace EvolutionSender.Models;

public class UsuarioSistema
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public int Ativo { get; set; } = 1;
    public int TentativasFalhas { get; set; }
    public DateTime? BloqueadoAte { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime? UltimoLoginEm { get; set; }

    public bool EstaAtivo => Ativo == 1;
    public bool EstaBloqueado => BloqueadoAte.HasValue && BloqueadoAte.Value > DateTime.Now;
}
