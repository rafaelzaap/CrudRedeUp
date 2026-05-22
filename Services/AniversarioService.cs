using System.Text.RegularExpressions;
using EvolutionSender.Data;
using EvolutionSender.Models;
using Microsoft.Extensions.Options;

namespace EvolutionSender.Services;

public partial class AniversarioService : IAniversarioService
{
    private const string TipoMensagemMembro = "mensagem_membro";
    private const string TipoLembreteAdmin = "lembrete_admin";
    private const string TipoLembreteGrupo = "lembrete_grupo";
    private const string ChaveEnvioAutomaticoAtivo = "aniversario.envio_automatico_ativo";
    private readonly IMembroRepository _membroRepository;
    private readonly IAniversarioEnvioHistoricoRepository _historicoRepository;
    private readonly ISistemaConfiguracaoRepository _configuracaoRepository;
    private readonly IEvolutionApiClient _evolutionApiClient;
    private readonly AniversarioOptions _options;

    public AniversarioService(
        IMembroRepository membroRepository,
        IAniversarioEnvioHistoricoRepository historicoRepository,
        ISistemaConfiguracaoRepository configuracaoRepository,
        IEvolutionApiClient evolutionApiClient,
        IOptions<AniversarioOptions> options)
    {
        _membroRepository = membroRepository;
        _historicoRepository = historicoRepository;
        _configuracaoRepository = configuracaoRepository;
        _evolutionApiClient = evolutionApiClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<AniversarianteViewModel>> ListarProximosAsync(
        CancellationToken cancellationToken = default)
    {
        var membros = await _membroRepository.ListarAniversariantesAsync();
        var hoje = DateTime.Today;
        var dataLimite = hoje.AddMonths(Math.Max(1, _options.MesesListagem));

        return membros
            .Where(m => m.EstaAtivo)
            .Select(m => CriarAniversariante(m, hoje))
            .Where(a => a.ProximoAniversario <= dataLimite)
            .OrderBy(a => a.DiasRestantes)
            .ThenBy(a => a.Nome)
            .ToList();
    }

    public async Task<IReadOnlyList<AniversarioEnvioHistorico>> ListarHistoricoAsync(
        CancellationToken cancellationToken = default)
    {
        return await _historicoRepository.ListarRecentesAsync(20, cancellationToken);
    }

    public async Task<bool> ObterEnvioAutomaticoAtivoAsync(CancellationToken cancellationToken = default)
    {
        var valor = await _configuracaoRepository.ObterAsync(ChaveEnvioAutomaticoAtivo, cancellationToken);
        return string.Equals(valor, "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task DefinirEnvioAutomaticoAtivoAsync(bool ativo, CancellationToken cancellationToken = default)
    {
        await _configuracaoRepository.SalvarAsync(
            ChaveEnvioAutomaticoAtivo,
            ativo ? "true" : "false",
            cancellationToken);
    }

    public async Task<bool> ExisteAniversarianteHojeAsync(CancellationToken cancellationToken = default)
    {
        var membros = await _membroRepository.ListarAniversariantesAsync();
        return membros.Any(m => m.EstaAtivo
            && CriarDataAniversario(DateTime.Today.Year, m.DataDeNascimento) == DateTime.Today);
    }

    public async Task<AniversarioEnvioResultado> EnviarMensagemParaMembroAsync(
        int membroCodigo,
        CancellationToken cancellationToken = default)
    {
        var membro = await _membroRepository.ObterPorCodigoAsync(membroCodigo);

        if (membro is null || !membro.EstaAtivo)
        {
            return new AniversarioEnvioResultado { Erros = ["Membro ativo nao encontrado."] };
        }

        return await EnviarParaMembroAsync(membro, registrarDuplicidade: false, cancellationToken);
    }

    public async Task<AniversarioEnvioResultado> EnviarLembreteAsync(
        int membroCodigo,
        CancellationToken cancellationToken = default)
    {
        var membro = await _membroRepository.ObterPorCodigoAsync(membroCodigo);

        if (membro is null || !membro.EstaAtivo)
        {
            return new AniversarioEnvioResultado { Erros = ["Membro ativo nao encontrado."] };
        }

        return await EnviarLembreteAsync(membro, registrarDuplicidade: false, cancellationToken);
    }

    public async Task<AniversarioEnvioResultado> EnviarAutomaticoHojeAsync(
        CancellationToken cancellationToken = default)
    {
        var membros = await _membroRepository.ListarAniversariantesAsync();
        var aniversariantesHoje = membros
            .Where(m => m.EstaAtivo
                && CriarDataAniversario(DateTime.Today.Year, m.DataDeNascimento) == DateTime.Today)
            .ToList();

        var enviados = 0;
        var ignorados = 0;
        var erros = new List<string>();

        foreach (var membro in aniversariantesHoje)
        {
            var lembrete = await EnviarLembreteAsync(membro, registrarDuplicidade: true, cancellationToken);
            var lembreteGrupo = await EnviarLembreteParaGrupoAsync(membro, registrarDuplicidade: true, cancellationToken);
            var mensagem = await EnviarParaMembroAsync(membro, registrarDuplicidade: true, cancellationToken);

            enviados += lembrete.TotalEnviados + lembreteGrupo.TotalEnviados + mensagem.TotalEnviados;
            ignorados += lembrete.TotalIgnorados + lembreteGrupo.TotalIgnorados + mensagem.TotalIgnorados;
            erros.AddRange(lembrete.Erros);
            erros.AddRange(lembreteGrupo.Erros);
            erros.AddRange(mensagem.Erros);
        }

        return new AniversarioEnvioResultado
        {
            TotalEncontrados = aniversariantesHoje.Count,
            TotalEnviados = enviados,
            TotalIgnorados = ignorados,
            Erros = erros
        };
    }

    private async Task<AniversarioEnvioResultado> EnviarParaMembroAsync(
        Membro membro,
        bool registrarDuplicidade,
        CancellationToken cancellationToken)
    {
        var numero = NormalizarTelefone(membro.Telefone);
        return await EnviarAsync(
            membro,
            numero,
            TipoMensagemMembro,
            MontarTexto(_options.MensagemAniversariante, membro),
            registrarDuplicidade,
            cancellationToken);
    }

    private async Task<AniversarioEnvioResultado> EnviarLembreteAsync(
        Membro membro,
        bool registrarDuplicidade,
        CancellationToken cancellationToken)
    {
        var numero = NormalizarTelefone(_options.NumeroLembrete);
        return await EnviarAsync(
            membro,
            numero,
            TipoLembreteAdmin,
            MontarTexto(_options.MensagemLembrete, membro),
            registrarDuplicidade,
            cancellationToken);
    }

    private async Task<AniversarioEnvioResultado> EnviarLembreteParaGrupoAsync(
        Membro membro,
        bool registrarDuplicidade,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.GrupoLembreteJid))
        {
            return new AniversarioEnvioResultado { TotalEncontrados = 1, TotalIgnorados = 1 };
        }

        return await EnviarAsync(
            membro,
            _options.GrupoLembreteJid,
            TipoLembreteGrupo,
            MontarTexto(_options.MensagemLembrete, membro),
            registrarDuplicidade,
            cancellationToken);
    }

    private async Task<AniversarioEnvioResultado> EnviarAsync(
        Membro membro,
        string? numero,
        string tipo,
        string texto,
        bool evitarDuplicidade,
        CancellationToken cancellationToken)
    {
        var dataReferencia = CriarDataAniversario(DateTime.Today.Year, membro.DataDeNascimento);

        if (evitarDuplicidade
            && await _historicoRepository.JaEnviadoAsync(membro.Codigo, tipo, dataReferencia, cancellationToken))
        {
            return new AniversarioEnvioResultado
            {
                TotalEncontrados = 1,
                TotalIgnorados = 1
            };
        }

        if (string.IsNullOrWhiteSpace(numero))
        {
            var erroTelefone = $"{membro.Nome}: telefone invalido.";
            await RegistrarAsync(membro, tipo, dataReferencia, string.Empty, "telefone_invalido", erroTelefone, cancellationToken);
            return new AniversarioEnvioResultado
            {
                TotalEncontrados = 1,
                Erros = [erroTelefone]
            };
        }

        var resultado = await _evolutionApiClient.EnviarTextoAsync(numero, texto, cancellationToken);

        if (resultado.Sucesso)
        {
            await RegistrarAsync(membro, tipo, dataReferencia, numero, "enviado", null, cancellationToken);
            return new AniversarioEnvioResultado { TotalEncontrados = 1, TotalEnviados = 1 };
        }

        var erro = resultado.Erro ?? "erro desconhecido";
        await RegistrarAsync(membro, tipo, dataReferencia, numero, "erro", erro, cancellationToken);
        return new AniversarioEnvioResultado
        {
            TotalEncontrados = 1,
            Erros = [$"{membro.Nome}: {erro}"]
        };
    }

    private async Task RegistrarAsync(
        Membro membro,
        string tipo,
        DateTime dataReferencia,
        string telefoneDestino,
        string status,
        string? erro,
        CancellationToken cancellationToken)
    {
        await _historicoRepository.RegistrarAsync(new AniversarioEnvioHistorico
        {
            MembroCodigo = membro.Codigo,
            Tipo = tipo,
            DataReferencia = dataReferencia.Date,
            TelefoneDestino = telefoneDestino,
            Status = status,
            Erro = erro
        }, cancellationToken);
    }

    private static AniversarianteViewModel CriarAniversariante(Membro membro, DateTime hoje)
    {
        var proximo = CriarDataAniversario(hoje.Year, membro.DataDeNascimento);

        if (proximo < hoje)
        {
            proximo = CriarDataAniversario(hoje.Year + 1, membro.DataDeNascimento);
        }

        return new AniversarianteViewModel
        {
            Codigo = membro.Codigo,
            Nome = membro.Nome,
            PrimeiroNome = membro.PrimeiroNome,
            Telefone = membro.Telefone,
            DataDeNascimento = membro.DataDeNascimento,
            ProximoAniversario = proximo,
            DiasRestantes = (proximo - hoje).Days,
            IdadeNova = proximo.Year - membro.DataDeNascimento.Year
        };
    }

    private static DateTime CriarDataAniversario(int ano, DateTime dataDeNascimento)
    {
        var dia = dataDeNascimento.Day;

        if (dataDeNascimento.Month == 2
            && dataDeNascimento.Day == 29
            && !DateTime.IsLeapYear(ano))
        {
            dia = 28;
        }

        return new DateTime(ano, dataDeNascimento.Month, dia);
    }

    private static string MontarTexto(string template, Membro membro)
    {
        return template
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

        if (digitos.StartsWith("55", StringComparison.Ordinal)
            && (digitos.Length == 12 || digitos.Length == 13))
        {
            return digitos;
        }

        if (digitos.Length == 10 || digitos.Length == 11)
        {
            return $"55{digitos}";
        }

        if (digitos.Length == 8 || digitos.Length == 9)
        {
            return $"5522{digitos}";
        }

        return null;
    }

    [GeneratedRegex(@"\D+")]
    private static partial Regex ApenasDigitosRegex();
}
