using System.Security.Claims;
using EvolutionSender.Models;
using EvolutionSender.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EvolutionSender.Controllers;

[AllowAnonymous]
public class ContaController : Controller
{
    private readonly IUsuarioAuthService _usuarioAuthService;

    public ContaController(IUsuarioAuthService usuarioAuthService)
    {
        _usuarioAuthService = usuarioAuthService;
    }

    public async Task<IActionResult> Login(string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirecionarLocal(returnUrl);
        }

        if (await _usuarioAuthService.PrecisaConfigurarAdminAsync(cancellationToken))
        {
            return RedirectToAction(nameof(Setup));
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _usuarioAuthService.EntrarAsync(model.Email, model.Senha, cancellationToken);

        if (!resultado.Sucesso || resultado.Usuario is null)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "E-mail ou senha invalidos.");
            return View(model);
        }

        await EntrarUsuarioAsync(resultado.Usuario, model.Lembrar);
        return RedirecionarLocal(model.ReturnUrl);
    }

    public async Task<IActionResult> Setup(CancellationToken cancellationToken = default)
    {
        if (!await _usuarioAuthService.PrecisaConfigurarAdminAsync(cancellationToken))
        {
            return RedirectToAction(nameof(Login));
        }

        return View(new SetupAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Setup(SetupAdminViewModel model, CancellationToken cancellationToken = default)
    {
        if (!await _usuarioAuthService.PrecisaConfigurarAdminAsync(cancellationToken))
        {
            return RedirectToAction(nameof(Login));
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var resultado = await _usuarioAuthService.CriarPrimeiroAdminAsync(
            model.Nome,
            model.Email,
            model.Senha,
            cancellationToken);

        if (!resultado.Sucesso)
        {
            ModelState.AddModelError(string.Empty, resultado.Erro ?? "Nao foi possivel criar o administrador.");
            return View(model);
        }

        TempData["Mensagem"] = "Administrador criado com sucesso. Entre para continuar.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    private async Task EntrarUsuarioAsync(UsuarioSistema usuario, bool lembrar)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.Nome),
            new(ClaimTypes.Email, usuario.Email)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var propriedades = new AuthenticationProperties
        {
            IsPersistent = lembrar,
            ExpiresUtc = DateTimeOffset.UtcNow.Add(lembrar ? TimeSpan.FromDays(14) : TimeSpan.FromHours(8))
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, propriedades);
    }

    private IActionResult RedirecionarLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Mensagens");
    }
}
