using EvolutionSender.Data;
using EvolutionSender.Models;
using EvolutionSender.Services;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionSender.Controllers;

public class MensagensController : Controller
{
    private readonly IMensagemRepository _mensagemRepository;
    private readonly IEnvioMensagemHistoricoRepository _historicoRepository;
    private readonly IEnvioMensagemJobService _envioMensagemJobService;

    public MensagensController(
        IMensagemRepository mensagemRepository,
        IEnvioMensagemHistoricoRepository historicoRepository,
        IEnvioMensagemJobService envioMensagemJobService)
    {
        _mensagemRepository = mensagemRepository;
        _historicoRepository = historicoRepository;
        _envioMensagemJobService = envioMensagemJobService;
    }

    public async Task<IActionResult> Index(string? busca, bool incluirInativas = false, CancellationToken cancellationToken = default)
    {
        ViewData["Busca"] = busca;
        ViewData["IncluirInativas"] = incluirInativas;
        ViewData["Envios"] = _envioMensagemJobService.ListarRecentes();
        ViewData["Historico"] = await _historicoRepository.ListarRecentesAsync(10, cancellationToken: cancellationToken);

        var mensagens = await _mensagemRepository.ListarAsync(busca, incluirInativas);
        return View(mensagens);
    }

    public async Task<IActionResult> Details(int id)
    {
        var mensagem = await _mensagemRepository.ObterPorIdAsync(id);

        if (mensagem is null)
        {
            return NotFound();
        }

        ViewData["Envios"] = _envioMensagemJobService.ListarRecentes(mensagemId: id);
        ViewData["Historico"] = await _historicoRepository.ListarRecentesAsync(10, id);

        return View(mensagem);
    }

    public IActionResult Create()
    {
        return View(new Mensagem
        {
            Ativo = 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Mensagem mensagem)
    {
        if (!ModelState.IsValid)
        {
            return View(mensagem);
        }

        var id = await _mensagemRepository.CriarAsync(mensagem);
        TempData["Mensagem"] = "Mensagem cadastrada com sucesso.";

        return RedirectToAction(nameof(Details), new { id });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var mensagem = await _mensagemRepository.ObterPorIdAsync(id);

        if (mensagem is null)
        {
            return NotFound();
        }

        return View(mensagem);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Mensagem mensagem)
    {
        if (id != mensagem.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(mensagem);
        }

        var atualizada = await _mensagemRepository.AtualizarAsync(mensagem);

        if (!atualizada)
        {
            return NotFound();
        }

        TempData["Mensagem"] = "Mensagem atualizada com sucesso.";
        return RedirectToAction(nameof(Details), new { id = mensagem.Id });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var mensagem = await _mensagemRepository.ObterPorIdAsync(id);

        if (mensagem is null)
        {
            return NotFound();
        }

        return View(mensagem);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var excluida = await _mensagemRepository.ExcluirAsync(id);

        if (!excluida)
        {
            return NotFound();
        }

        TempData["Mensagem"] = "Mensagem inativada com sucesso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult EnviarAtiva(int? id)
    {
        _ = _envioMensagemJobService.Enfileirar(id);
        TempData["Mensagem"] = id.HasValue
            ? $"Envio da mensagem {id.Value} iniciado em segundo plano."
            : "Envio das mensagens ativas iniciado em segundo plano.";

        if (id.HasValue)
        {
            return RedirectToAction(nameof(Details), new { id = id.Value });
        }

        return RedirectToAction(nameof(Index));
    }
}
