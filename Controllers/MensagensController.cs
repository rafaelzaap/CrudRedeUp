using EvolutionSender.Data;
using EvolutionSender.Models;
using EvolutionSender.Services;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionSender.Controllers;

public class MensagensController : Controller
{
    private readonly IMensagemRepository _mensagemRepository;
    private readonly IEnvioMensagemService _envioMensagemService;

    public MensagensController(
        IMensagemRepository mensagemRepository,
        IEnvioMensagemService envioMensagemService)
    {
        _mensagemRepository = mensagemRepository;
        _envioMensagemService = envioMensagemService;
    }

    public async Task<IActionResult> Index(string? busca, bool incluirInativas = false)
    {
        ViewData["Busca"] = busca;
        ViewData["IncluirInativas"] = incluirInativas;

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
    public async Task<IActionResult> EnviarAtiva(int? id, CancellationToken cancellationToken)
    {
        var resultado = id.HasValue
            ? await _envioMensagemService.EnviarMensagemAtivaAsync(id.Value, cancellationToken)
            : await _envioMensagemService.EnviarTodasMensagensAtivasAsync(cancellationToken);

        if (resultado.TotalEnviados > 0)
        {
            TempData["Mensagem"] =
                $"Envio finalizado: {resultado.TotalMensagensInativadas}/{resultado.TotalMensagens} mensagem(ns) processada(s), {resultado.TotalEnviados} envio(s), {resultado.TotalErros} erro(s).";
        }
        else
        {
            TempData["Erro"] = resultado.Erros.FirstOrDefault() ?? "Nenhuma mensagem foi enviada.";
        }

        if (resultado.Erros.Count > 0)
        {
            TempData["DetalhesErro"] = string.Join(Environment.NewLine, resultado.Erros.Take(10));
        }

        if (id.HasValue)
        {
            return RedirectToAction(nameof(Details), new { id = id.Value });
        }

        return RedirectToAction(nameof(Index));
    }
}
