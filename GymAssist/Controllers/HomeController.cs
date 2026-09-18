using System.Diagnostics;
using System.Data.Common;
using GymAssist.Data;
using GymAssist.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GymAssist.Controllers;

public class HomeController(
    GymAssistDbContext dbContext,
    IOptions<CheckInOptions> checkInOptions,
    ILogger<HomeController> logger) : Controller
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
        ModelState.Remove(nameof(model.Identificador));
        model.Identificador = string.Empty;
        var idCliente = int.TryParse(identificador, out var parsedId) ? parsedId : (int?)null;
        var email = identificador.ToLowerInvariant();
        var databaseOperation = "buscar el cliente";
        try
        {
            var clientesQuery = dbContext.Clientes.AsNoTracking();
            if (parsedId > 0)
            {
                clientesQuery = clientesQuery.Where(cliente => cliente.Activo &&
                    (cliente.IdCliente == parsedId ||
                     cliente.Cedula == identificador ||
                     (cliente.Email != null && cliente.Email.ToLower() == email) ||
                     cliente.Telefono == identificador));
            }
            else
            {
                clientesQuery = clientesQuery.Where(cliente => cliente.Activo &&
                    (cliente.Cedula == identificador ||
                     (cliente.Email != null && cliente.Email.ToLower() == email) ||
                     cliente.Telefono == identificador));
            }

            var cliente = await clientesQuery.FirstOrDefaultAsync();

            if (cliente is null)
            {
                SetResult(model, "Usuario no encontrado", "No encontramos un cliente activo con esos datos.", null, "danger");
                return View("Index", model);
            }

            databaseOperation = "consultar el pago vigente";
            var hoy = DateTime.Today;
            var pago = await dbContext.Pagos
                .AsNoTracking()
                .Where(pago => pago.IdCliente == cliente.IdCliente &&
                    pago.Estado == "pagado" &&
                    pago.FechaInicio <= hoy &&
                    pago.FechaFin >= hoy)
                .OrderByDescending(pago => pago.FechaFin)
                .FirstOrDefaultAsync();

            if (pago is null)
            {
                databaseOperation = "consultar pagos pendientes";
                var pagoPendiente = await dbContext.Pagos
                    .AsNoTracking()
                    .Where(candidate => candidate.IdCliente == cliente.IdCliente && candidate.Estado == "pendiente")
                    .OrderByDescending(candidate => candidate.FechaFin)
                    .FirstOrDefaultAsync();

                if (pagoPendiente is not null)
                {
                    SetResult(model, "Pago pendiente", $"Hola, {cliente.Nombres}.", "Tienes un pago pendiente. Acércate a recepción para regularizarlo.", "warning");
                    return View("Index", model);
                }

                databaseOperation = "consultar el último pago";
                var ultimoPago = await dbContext.Pagos
                    .AsNoTracking()
                    .Where(candidate => candidate.IdCliente == cliente.IdCliente && candidate.Estado != "cancelado")
                    .OrderByDescending(candidate => candidate.FechaFin)
                    .FirstOrDefaultAsync();

                if (ultimoPago is null)
                {
                    SetResult(model, "Sin suscripción", $"Hola, {cliente.Nombres}.", "No tienes una suscripción registrada.", "danger");
                }
                else if (ultimoPago.FechaInicio > hoy)
                {
                    SetResult(model, "Suscripción programada", $"Hola, {cliente.Nombres}.", $"Tu suscripción inicia el {ultimoPago.FechaInicio:dd/MM/yyyy}.", "warning");
                }
                else
                {
                    SetResult(model, "Suscripción vencida", $"Hola, {cliente.Nombres}.", $"Tu suscripción venció el {ultimoPago.FechaFin:dd/MM/yyyy}.", "danger");
                }
                return View("Index", model);
            }

            if (pago.FechaFin <= hoy.AddDays(checkInOptions.Value.DiasProximoVencimiento))
            {
                SetResult(model, "Suscripción próxima a vencer", $"Hola, {cliente.Nombres}.", $"Tu suscripción vence el {pago.FechaFin:dd/MM/yyyy}.", "warning");
            }
            else
            {
                SetResult(model, $"Bienvenido a {model.GymName}", $"Hola, {cliente.Nombres}.", "Tu suscripción está al día. Puedes ingresar.", "success");
            }

            return View("Index", model);
        }
        catch (Exception exception)
        {
            var requestId = HttpContext.TraceIdentifier;
            logger.LogError(
                exception,
                "Error durante el check-in. Operación: {DatabaseOperation}. RequestId: {RequestId}",
                databaseOperation,
                requestId);
            var errorType = exception.GetBaseException().GetType().Name;
            SetResult(model, "No pudimos verificar tu acceso", "Ocurrió un error al consultar tu información.", $"Tipo: {errorType}. Código de diagnóstico: {requestId}.", "danger");
            return View("Index", model);
        }
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
        model.Identificador = string.Empty;
        model.ResultTitle = title;
        model.ResultMessage = message;
        model.ResultDetail = detail;
        model.ResultLevel = level;
    }

    private static bool IsDatabaseException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is DbException or NpgsqlException)
            {
                return true;
            }
        }

        return false;
    }
}

public class CheckInOptions
{
    public int DiasProximoVencimiento { get; set; } = 5;
}
