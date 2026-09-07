using System.Security.Claims;
using GymAssist.Data;
using GymAssist.Models;
using GymAssist.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace GymAssist.Controllers;

public class AdminController(GymAssistDbContext dbContext, AesPasswordService passwordService) : Controller
{
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction(nameof(Index));
        }

        return View(new AdminLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Usuario.Trim().ToLowerInvariant();
        var usuario = await dbContext.Usuarios
            .FirstOrDefaultAsync(candidate => candidate.Activo &&
                (candidate.Rol == "admin" || candidate.Rol == "super_admin") &&
                candidate.Email.ToLower() == email);

        if (usuario is null || !passwordService.Verify(model.Password, usuario.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Las credenciales no son válidas.");
            return View(model);
        }

        usuario.UltimoAcceso = DateTime.UtcNow;
        await dbContext.SaveChangesAsync();

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.Nombre),
            new Claim(ClaimTypes.Email, usuario.Email),
            new Claim(ClaimTypes.Role, usuario.Rol)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult Usuarios()
    {
        ViewData["ModuleTitle"] = "Gestión de usuarios";
        ViewData["ModuleDescription"] = "Administra las cuentas y permisos del equipo.";
        return View("Module");
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult Clientes()
    {
        ViewData["ModuleTitle"] = "Gestión de clientes";
        ViewData["ModuleDescription"] = "Consulta y administra los clientes del gimnasio.";
        return View("Module");
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult Pagos()
    {
        ViewData["ModuleTitle"] = "Gestión de pagos";
        ViewData["ModuleDescription"] = "Registra y consulta los pagos de membresías.";
        return View("Module");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
