using System.ComponentModel.DataAnnotations;

namespace EvolutionSender.Models;

public class Membro
{
    public int Codigo { get; set; }

    [Required(ErrorMessage = "Informe o nome completo.")]
    [StringLength(150, ErrorMessage = "O nome deve ter no maximo 150 caracteres.")]
    [Display(Name = "Nome completo")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a data de nascimento.")]
    [DataType(DataType.Date)]
    [Display(Name = "Data de nascimento")]
    public DateTime DataDeNascimento { get; set; }

    [StringLength(20, ErrorMessage = "O telefone deve ter no maximo 20 caracteres.")]
    public string? Telefone { get; set; }

    [StringLength(255, ErrorMessage = "A observacao deve ter no maximo 255 caracteres.")]
    [Display(Name = "Observacao")]
    public string? Observacao { get; set; }

    [StringLength(100, ErrorMessage = "O primeiro nome deve ter no maximo 100 caracteres.")]
    [Display(Name = "Primeiro nome")]
    public string PrimeiroNome { get; set; } = string.Empty;

    [Display(Name = "Ativo")]
    public int Ativo { get; set; } = 1;

    public bool EstaAtivo => Ativo == 1;
}
