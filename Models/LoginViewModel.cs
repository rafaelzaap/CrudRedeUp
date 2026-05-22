using System.ComponentModel.DataAnnotations;

namespace EvolutionSender.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Informe o e-mail.")]
    [EmailAddress(ErrorMessage = "Informe um e-mail valido.")]
    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    [DataType(DataType.Password)]
    public string Senha { get; set; } = string.Empty;

    [Display(Name = "Manter conectado")]
    public bool Lembrar { get; set; }

    public string? ReturnUrl { get; set; }
}
