using System.Diagnostics;
using GymAssist.Data;
using GymAssist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GymAssist.Controllers;

public class HomeController(GymAssistDbContext dbContext, IOptions<CheckInOptions> checkInOptions) : Controller
{
    public IActionResult Index()
    {
        return View(new CheckInViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(CheckInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var identificador = model.Identificador.Trim();
        var idCliente = int.TryParse(identificador, out var parsedId) ? parsedId : (int?)null;
        var email = identificador.ToLowerInvariant();
        var cliente = await dbContext.Clientes
            .AsNoTracking()
            .FirstOrDefaultAsync(cliente => cliente.Activo &&
                (cliente.IdCliente == idCliente ||
                 cliente.Cedula == identificador ||
                 (cliente.Email != null && cliente.Email.ToLower() == email) ||
                 cliente.Telefono == identificador));

        if (cliente is null)
        {
            SetResult(model, "Usuario no encontrado", "No encontramos un cliente activo con esos datos.", null, "danger");
            return View("Index", model);
        }

        var pago = await dbContext.Pagos
            .AsNoTracking()
            .Where(pago => pago.IdCliente == cliente.IdCliente)
            .OrderByDescending(pago => pago.FechaFin)
            .FirstOrDefaultAsync();

        if (pago is null)
        {
            SetResult(model, "Sin suscripción", $"Hola, {cliente.Nombres}.", "No tienes una suscripción registrada.", "danger");
            return View("Index", model);
        }

        if (pago.Estado == "pendiente")
        {
            SetResult(model, "Pago pendiente", $"Hola, {cliente.Nombres}.", "Tienes un pago pendiente. Acércate a recepción para regularizarlo.", "warning");
        }
        else if (pago.FechaFin < DateTime.Today || pago.Estado is "vencido" or "cancelado")
        {
            SetResult(model, "Suscripción vencida", $"Hola, {cliente.Nombres}.", $"Tu suscripción venció el {pago.FechaFin:dd/MM/yyyy}.", "danger");
        }
        else if (pago.FechaFin <= DateTime.Today.AddDays(checkInOptions.Value.DiasProximoVencimiento))
        {
            SetResult(model, "Suscripción próxima a vencer", $"Hola, {cliente.Nombres}.", $"Tu suscripción vence el {pago.FechaFin:dd/MM/yyyy}.", "warning");
        }
        else
        {
            SetResult(model, $"Bienvenido a {model.GymName}", $"Hola, {cliente.Nombres}.", "Tu suscripción está al día. Puedes ingresar.", "success");
        }

        return View("Index", model);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static void SetResult(CheckInViewModel model, string title, string message, string? detail, string level)
    {
        model.ResultTitle = title;
        model.ResultMessage = message;
        model.ResultDetail = detail;
        model.ResultLevel = level;
    }
}

public class CheckInOptions
{
    public int DiasProximoVencimiento { get; set; } = 5;
}
