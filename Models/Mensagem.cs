using System.ComponentModel.DataAnnotations;

namespace EvolutionSender.Models;

public class Mensagem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Informe o titulo.")]
    [StringLength(150, ErrorMessage = "O titulo deve ter no maximo 150 caracteres.")]
    [Display(Name = "Titulo")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a ocasiao.")]
    [StringLength(100, ErrorMessage = "A ocasiao deve ter no maximo 100 caracteres.")]
    [Display(Name = "Ocasiao")]
    public string Ocasiao { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a mensagem.")]
    [Display(Name = "Mensagem")]
    public string Texto { get; set; } = string.Empty;

    [Display(Name = "Ativa")]
    public int Ativo { get; set; } = 1;

    [Display(Name = "Criada em")]
    public DateTime? CriadoEm { get; set; }

    [Display(Name = "Atualizada em")]
    public DateTime? AtualizadoEm { get; set; }

    public bool EstaAtiva => Ativo == 1;
}
