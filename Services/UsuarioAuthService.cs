using System.Security.Cryptography;
using EvolutionSender.Data;
using EvolutionSender.Models;

namespace EvolutionSender.Services;

public class UsuarioAuthService : IUsuarioAuthService
{
    private const int MaxTentativas = 5;
    private static readonly TimeSpan TempoBloqueio = TimeSpan.FromMinutes(15);
    private readonly IUsuarioSistemaRepository _usuarioRepository;

    public UsuarioAuthService(IUsuarioSistemaRepository usuarioRepository)
    {
        _usuarioRepository = usuarioRepository;
    }

    public async Task<bool> PrecisaConfigurarAdminAsync(CancellationToken cancellationToken = default)
    {
        return !await _usuarioRepository.ExisteUsuarioAsync(cancellationToken);
    }

    public async Task<(bool Sucesso, string? Erro, UsuarioSistema? Usuario)> EntrarAsync(
        string email,
        string senha,
        CancellationToken cancellationToken = default)
    {
        var usuario = await _usuarioRepository.ObterPorEmailAsync(email, cancellationToken);

        if (usuario is null || !usuario.EstaAtivo)
        {
            return (false, "E-mail ou senha invalidos.", null);
        }

        if (usuario.EstaBloqueado)
        {
            return (false, $"Usuario bloqueado ate {usuario.BloqueadoAte:dd/MM/yyyy HH:mm}.", null);
        }

        if (!VerificarSenha(senha, usuario.SenhaHash))
        {
            var tentativas = usuario.TentativasFalhas + 1;
            var bloquearAte = tentativas >= MaxTentativas ? DateTime.Now.Add(TempoBloqueio) : (DateTime?)null;

            await _usuarioRepository.RegistrarFalhaLoginAsync(usuario.Id, bloquearAte, cancellationToken);
            return (false, "E-mail ou senha invalidos.", null);
        }

        await _usuarioRepository.RegistrarLoginSucessoAsync(usuario.Id, cancellationToken);
        return (true, null, usuario);
    }

    public async Task<(bool Sucesso, string? Erro)> CriarPrimeiroAdminAsync(
        string nome,
        string email,
        string senha,
        CancellationToken cancellationToken = default)
    {
        if (!await PrecisaConfigurarAdminAsync(cancellationToken))
        {
            return (false, "O administrador inicial ja foi configurado.");
        }

        var erroSenha = ValidarSenhaForte(senha);

        if (erroSenha is not null)
        {
            return (false, erroSenha);
        }

        await _usuarioRepository.CriarAsync(new UsuarioSistema
        {
            Nome = nome.Trim(),
            Email = email.Trim().ToLowerInvariant(),
            SenhaHash = CriarHashSenha(senha),
            Ativo = 1
        }, cancellationToken);

        return (true, null);
    }

    private static string? ValidarSenhaForte(string senha)
    {
        if (senha.Length < 10)
        {
            return "A senha deve ter pelo menos 10 caracteres.";
        }

        if (!senha.Any(char.IsUpper)
            || !senha.Any(char.IsLower)
            || !senha.Any(char.IsDigit)
            || !senha.Any(c => !char.IsLetterOrDigit(c)))
        {
            return "Use maiusculas, minusculas, numeros e simbolos na senha.";
        }

        return null;
    }

    private static string CriarHashSenha(string senha)
    {
        const int iteracoes = 210_000;
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            iteracoes,
            HashAlgorithmName.SHA256,
            32);

        return $"PBKDF2-SHA256${iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    private static bool VerificarSenha(string senha, string senhaHash)
    {
        var partes = senhaHash.Split('$');

        if (partes.Length != 4
            || partes[0] != "PBKDF2-SHA256"
            || !int.TryParse(partes[1], out var iteracoes))
        {
            return false;
        }

        var salt = Convert.FromBase64String(partes[2]);
        var hashEsperado = Convert.FromBase64String(partes[3]);
        var hashInformado = Rfc2898DeriveBytes.Pbkdf2(
            senha,
            salt,
            iteracoes,
            HashAlgorithmName.SHA256,
            hashEsperado.Length);

        return CryptographicOperations.FixedTimeEquals(hashEsperado, hashInformado);
    }
}
