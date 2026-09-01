using GymAssist.Data;
using GymAssist.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymAssist.Controllers;

public class TestDBController(GymAssistDbContext dbContext) : Controller
{
    public async Task<IActionResult> Index()
    {
        try
        {
            var usuarios = await dbContext.Usuarios
                .AsNoTracking()
                .Select(usuario => new UsuarioDto
                {
                    IdUsuario = usuario.IdUsuario,
                    Nombre = usuario.Nombre,
                    Email = usuario.Email,
                    Rol = usuario.Rol,
                    Activo = usuario.Activo
                })
                .ToListAsync();

            return View(usuarios);
        }
        catch (Exception exception)
        {
            ViewBag.Error = exception.Message;
            return View(Array.Empty<UsuarioDto>());
        }
    }
}
