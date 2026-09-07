using System.Security.Claims;
using GymAssist.Data;
using GymAssist.Models;
using GymAssist.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

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
        Usuario? usuario;
        try
        {
            usuario = await dbContext.Usuarios
                .FirstOrDefaultAsync(candidate => candidate.Activo &&
                    (candidate.Rol == "admin" || candidate.Rol == "super_admin") &&
                    candidate.Email.ToLower() == email);
        }
        catch (Exception exception) when (IsDatabaseException(exception))
        {
            ModelState.AddModelError(string.Empty, "No se pudo conectar con la base de datos. Verifica que PostgreSQL esté activo y configurado.");
            HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>()
                .LogError(exception, "No fue posible consultar el usuario administrador durante el login.");
            return View(model);
        }

        if (usuario is null || !passwordService.Verify(model.Password, usuario.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Las credenciales no son válidas.");
            return View(model);
        }

        try
        {
            usuario.UltimoAcceso = DateTime.UtcNow;
            await dbContext.SaveChangesAsync();
        }
        catch (Exception exception) when (IsDatabaseException(exception))
        {
            ModelState.AddModelError(string.Empty, "No se pudo completar el inicio de sesión porque la base de datos no está disponible.");
            HttpContext.RequestServices.GetRequiredService<ILogger<AdminController>>()
                .LogError(exception, "No fue posible actualizar el último acceso del administrador.");
            return View(model);
        }

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

    private static bool IsDatabaseException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is NpgsqlException)
            {
                return true;
            }
        }

        return false;
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult Index()
    {
        return View();
    }

    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Usuarios()
    {
        var usuarios = await dbContext.Usuarios
            .AsNoTracking()
            .OrderBy(usuario => usuario.Nombre)
            .ToListAsync();

        return View(usuarios);
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult CrearUsuario()
    {
        return View(new AdminUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> CrearUsuario(AdminUserViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Password))
        {
            ModelState.AddModelError(nameof(model.Password), "La contraseña es obligatoria.");
        }

        var email = model.Email.Trim().ToLowerInvariant();
        if (await dbContext.Usuarios.AnyAsync(usuario => usuario.Email.ToLower() == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese correo.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        dbContext.Usuarios.Add(new Usuario
        {
            Nombre = model.Nombre.Trim(),
            Email = email,
            PasswordHash = passwordService.Encrypt(model.Password!),
            Rol = model.Rol,
            Activo = model.Activo,
            FechaCreacion = DateTime.UtcNow
        });
        await dbContext.SaveChangesAsync();

        TempData["AdminNotice"] = "Usuario creado correctamente.";
        return RedirectToAction(nameof(Usuarios));
    }

    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EditarUsuario(int id)
    {
        var usuario = await dbContext.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        return View(new AdminUserViewModel
        {
            IdUsuario = usuario.IdUsuario,
            Nombre = usuario.Nombre,
            Email = usuario.Email,
            Rol = usuario.Rol,
            Activo = usuario.Activo
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EditarUsuario(AdminUserViewModel model)
    {
        var usuario = await dbContext.Usuarios.FindAsync(model.IdUsuario);
        if (usuario is null)
        {
            return NotFound();
        }

        var email = model.Email.Trim().ToLowerInvariant();
        if (await dbContext.Usuarios.AnyAsync(candidate =>
                candidate.IdUsuario != model.IdUsuario && candidate.Email.ToLower() == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Ya existe un usuario con ese correo.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        usuario.Nombre = model.Nombre.Trim();
        usuario.Email = email;
        usuario.Rol = model.Rol;
        usuario.Activo = model.Activo;
        if (!string.IsNullOrWhiteSpace(model.Password))
        {
            usuario.PasswordHash = passwordService.Encrypt(model.Password);
        }

        await dbContext.SaveChangesAsync();
        TempData["AdminNotice"] = "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Usuarios));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EliminarUsuario(int id)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (currentUserId == id.ToString())
        {
            TempData["AdminError"] = "No puedes desactivar tu propio usuario.";
            return RedirectToAction(nameof(Usuarios));
        }

        var usuario = await dbContext.Usuarios.FindAsync(id);
        if (usuario is null)
        {
            return NotFound();
        }

        usuario.Activo = false;
        await dbContext.SaveChangesAsync();
        TempData["AdminNotice"] = "Usuario desactivado correctamente.";
        return RedirectToAction(nameof(Usuarios));
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
