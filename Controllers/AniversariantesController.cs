using EvolutionSender.Services;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionSender.Controllers;

public class AniversariantesController : Controller
{
    private readonly IAniversarioService _aniversarioService;

    public AniversariantesController(IAniversarioService aniversarioService)
    {
        _aniversarioService = aniversarioService;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ViewData["Historico"] = await _aniversarioService.ListarHistoricoAsync(cancellationToken);
        ViewData["EnvioAutomaticoAtivo"] = await _aniversarioService.ObterEnvioAutomaticoAtivoAsync(cancellationToken);
        ViewData["ExisteAniversarianteHoje"] = await _aniversarioService.ExisteAniversarianteHojeAsync(cancellationToken);
        var aniversariantes = await _aniversarioService.ListarProximosAsync(cancellationToken);
        return View(aniversariantes);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AlternarAutomatico(bool ativo, CancellationToken cancellationToken)
    {
        await _aniversarioService.DefinirEnvioAutomaticoAtivoAsync(ativo, cancellationToken);
        TempData["Mensagem"] = ativo
            ? "Envio automatico de aniversarios ativado para 09:00."
            : "Envio automatico de aniversarios desativado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnviarAutomaticoHoje(CancellationToken cancellationToken)
    {
        var resultado = await _aniversarioService.EnviarAutomaticoHojeAsync(cancellationToken);

        if (resultado.TotalEncontrados == 0)
        {
            TempData["Erro"] = "Nenhum aniversariante encontrado para hoje.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mensagem"] =
            $"Envio manual finalizado: {resultado.TotalEncontrados} aniversariante(s), {resultado.TotalEnviados} envio(s), {resultado.TotalIgnorados} ignorado(s), {resultado.Erros.Count} erro(s).";

        if (resultado.Erros.Count > 0)
        {
            TempData["DetalhesErro"] = string.Join(Environment.NewLine, resultado.Erros.Take(10));
        }

        return RedirectToAction(nameof(Index));
    }

}
