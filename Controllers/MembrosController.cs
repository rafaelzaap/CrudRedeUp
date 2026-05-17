using EvolutionSender.Data;
using EvolutionSender.Models;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionSender.Controllers;

public class MembrosController : Controller
{
    private readonly IMembroRepository _membroRepository;

    public MembrosController(IMembroRepository membroRepository)
    {
        _membroRepository = membroRepository;
    }

    public async Task<IActionResult> Index(string? busca, bool incluirInativos = false)
    {
        ViewData["Busca"] = busca;
        ViewData["IncluirInativos"] = incluirInativos;

        var membros = await _membroRepository.ListarAsync(busca, incluirInativos);
        return View(membros);
    }

    public async Task<IActionResult> Details(int id)
    {
        var membro = await _membroRepository.ObterPorCodigoAsync(id);

        if (membro is null)
        {
            return NotFound();
        }

        return View(membro);
    }

    public IActionResult Create()
    {
        return View(new Membro
        {
            DataDeNascimento = DateTime.Today,
            Ativo = 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Membro membro)
    {
        if (!ModelState.IsValid)
        {
            return View(membro);
        }

        var codigo = await _membroRepository.CriarAsync(membro);
        TempData["Mensagem"] = "Membro cadastrado com sucesso.";

        return RedirectToAction(nameof(Details), new { id = codigo });
    }

    public async Task<IActionResult> Edit(int id)
    {
        var membro = await _membroRepository.ObterPorCodigoAsync(id);

        if (membro is null)
        {
            return NotFound();
        }

        return View(membro);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Membro membro)
    {
        if (id != membro.Codigo)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(membro);
        }

        var atualizado = await _membroRepository.AtualizarAsync(membro);

        if (!atualizado)
        {
            return NotFound();
        }

        TempData["Mensagem"] = "Cadastro atualizado com sucesso.";
        return RedirectToAction(nameof(Details), new { id = membro.Codigo });
    }

    public async Task<IActionResult> Delete(int id)
    {
        var membro = await _membroRepository.ObterPorCodigoAsync(id);

        if (membro is null)
        {
            return NotFound();
        }

        return View(membro);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var excluido = await _membroRepository.ExcluirAsync(id);

        if (!excluido)
        {
            return NotFound();
        }

        TempData["Mensagem"] = "Membro inativado com sucesso.";
        return RedirectToAction(nameof(Index));
    }
}
