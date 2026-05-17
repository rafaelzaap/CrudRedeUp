using System.Text.RegularExpressions;
using EvolutionSender.Data;
using EvolutionSender.Models;
using Microsoft.Extensions.Options;

namespace EvolutionSender.Services;

public partial class EnvioMensagemService : IEnvioMensagemService
{
    private readonly IMembroRepository _membroRepository;
    private readonly IMensagemRepository _mensagemRepository;
    private readonly IEvolutionApiClient _evolutionApiClient;
    private readonly ILogger<EnvioMensagemService> _logger;
    private readonly EnvioMensagemOptions _options;

    public EnvioMensagemService(
        IMembroRepository membroRepository,
        IMensagemRepository mensagemRepository,
        IEvolutionApiClient evolutionApiClient,
        ILogger<EnvioMensagemService> logger,
        IOptions<EnvioMensagemOptions> options)
    {
        _membroRepository = membroRepository;
        _mensagemRepository = mensagemRepository;
        _evolutionApiClient = evolutionApiClient;
        _logger = logger;
        _options = options.Value;
    }

    public async Task<EnvioMensagemResultado> EnviarMensagemAtivaAsync(CancellationToken cancellationToken = default)
    {
        var mensagem = await _mensagemRepository.ObterAtivaAsync();
        return await EnviarAsync(mensagem, cancellationToken);
    }

    public async Task<EnvioMensagemResultado> EnviarTodasMensagensAtivasAsync(
        CancellationToken cancellationToken = default)
    {
        var mensagens = await _mensagemRepository.ListarAtivasAsync();

        if (mensagens.Count == 0)
        {
            return new EnvioMensagemResultado
            {
                Erros = ["Nenhuma mensagem ativa encontrada."]
            };
        }

        var erros = new List<string>();
        var totalMembros = 0;
        var totalEnviados = 0;
        var totalInativadas = 0;

        for (var indice = 0; indice < mensagens.Count; indice++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var resultado = await EnviarAsync(mensagens[indice], cancellationToken);

            totalMembros += resultado.TotalMembros;
            totalEnviados += resultado.TotalEnviados;
            totalInativadas += resultado.MensagemInativada ? 1 : 0;
            erros.AddRange(resultado.Erros.Select(erro => $"Mensagem {mensagens[indice].Id}: {erro}"));

            if (indice < mensagens.Count - 1)
            {
                var delaySegundos = ObterDelaySegundos();
                _logger.LogInformation(
                    "Aguardando {DelaySegundos} segundo(s) antes da proxima mensagem ativa.",
                    delaySegundos);

                await Task.Delay(TimeSpan.FromSeconds(delaySegundos), cancellationToken);
            }
        }

        return new EnvioMensagemResultado
        {
            TotalMensagens = mensagens.Count,
            TotalMensagensInativadas = totalInativadas,
            TotalMembros = totalMembros,
            TotalEnviados = totalEnviados,
            TotalErros = erros.Count,
            MensagemInativada = totalInativadas == mensagens.Count,
            Erros = erros
        };
    }

    public async Task<EnvioMensagemResultado> EnviarMensagemAtivaAsync(
        int mensagemId,
        CancellationToken cancellationToken = default)
    {
        var mensagem = await _mensagemRepository.ObterAtivaPorIdAsync(mensagemId);
        return await EnviarAsync(mensagem, cancellationToken);
    }

    private async Task<EnvioMensagemResultado> EnviarAsync(
        Mensagem? mensagem,
        CancellationToken cancellationToken)
    {
        if (mensagem is null)
        {
            return new EnvioMensagemResultado
            {
                Erros = ["Nenhuma mensagem ativa encontrada."]
            };
        }

        var membros = await _membroRepository.ListarAsync(busca: null, incluirInativos: false);
        var erros = new List<string>();
        var enviados = 0;
        var membrosAtivos = membros.Where(m => m.EstaAtivo).ToList();

        for (var indice = 0; indice < membrosAtivos.Count; indice++)
        {
            var membro = membrosAtivos[indice];

            cancellationToken.ThrowIfCancellationRequested();

            var numero = NormalizarTelefone(membro.Telefone);

            if (string.IsNullOrWhiteSpace(numero))
            {
                var erroTelefone = $"{membro.Nome}: telefone vazio ou invalido.";
                erros.Add(erroTelefone);
                _logger.LogWarning(
                    "Envio ignorado por telefone invalido. Codigo: {Codigo}. Nome: {Nome}. TelefoneOriginal: {TelefoneOriginal}",
                    membro.Codigo,
                    membro.Nome,
                    membro.Telefone);
                continue;
            }

            var texto = MontarTexto(mensagem, membro);
            var resultado = await _evolutionApiClient.EnviarTextoAsync(numero, texto, cancellationToken);

            if (resultado.Sucesso)
            {
                enviados++;
            }
            else
            {
                var erro = resultado.Erro ?? "erro desconhecido";
                erros.Add($"{membro.Nome}: {erro}");
                _logger.LogError(
                    "Falha ao enviar mensagem. MensagemId: {MensagemId}. Codigo: {Codigo}. Nome: {Nome}. Numero: {Numero}. Erro: {Erro}",
                    mensagem.Id,
                    membro.Codigo,
                    membro.Nome,
                    numero,
                    erro);
            }

            if (indice < membrosAtivos.Count - 1)
            {
                var delaySegundos = ObterDelaySegundos();
                _logger.LogInformation(
                    "Aguardando {DelaySegundos} segundo(s) antes do proximo envio.",
                    delaySegundos);

                await Task.Delay(TimeSpan.FromSeconds(delaySegundos), cancellationToken);
            }
        }

        var inativada = false;

        if (enviados > 0)
        {
            inativada = await _mensagemRepository.InativarAsync(mensagem.Id);
        }

        return new EnvioMensagemResultado
        {
            MensagemId = mensagem.Id,
            TotalMensagens = 1,
            TotalMensagensInativadas = inativada ? 1 : 0,
            TotalMembros = membros.Count,
            TotalEnviados = enviados,
            TotalErros = erros.Count,
            MensagemInativada = inativada,
            Erros = erros
        };
    }

    private static string MontarTexto(Mensagem mensagem, Membro membro)
    {
        return mensagem.Texto
            .Replace("{nome}", membro.Nome)
            .Replace("{primeiro_nome}", membro.PrimeiroNome)
            .Replace("{telefone}", membro.Telefone ?? string.Empty);
    }

    private static string? NormalizarTelefone(string? telefone)
    {
        if (string.IsNullOrWhiteSpace(telefone))
        {
            return null;
        }

        var digitos = ApenasDigitosRegex().Replace(telefone, string.Empty);

        if (digitos.StartsWith("55", StringComparison.Ordinal))
        {
            return digitos;
        }

        return $"5522{digitos}";
    }

    private int ObterDelaySegundos()
    {
        var minimo = Math.Max(0, _options.DelayMinimoSegundos);
        var maximo = Math.Max(minimo, _options.DelayMaximoSegundos);

        return Random.Shared.Next(minimo, maximo + 1);
    }

    [GeneratedRegex(@"\D+")]
    private static partial Regex ApenasDigitosRegex();
}
