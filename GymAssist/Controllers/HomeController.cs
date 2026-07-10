using System.Diagnostics;
using GymAssist.Models;
using Microsoft.AspNetCore.Mvc;

namespace GymAssist.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        return View(new CheckInViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult CheckIn(CheckInViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        var paymentStatus = GetPaymentStatus(model.Identificador);
        TempData["CheckInTitle"] = $"Bienvenido a {model.GymName}";
        TempData["CheckInMessage"] = $"Tu ingreso se registró correctamente para {model.Identificador}.";
        TempData["CheckInStatus"] = paymentStatus;
        TempData["CheckInDetail"] = paymentStatus switch
        {
            "Pago atrasado" => "Tu membresía tiene un pago atrasado. Puedes acercarte a recepción para regularizarlo.",
            "Próximo a vencer" => "Tu membresía está próxima a vencer. Te recomendamos revisar tu renovación.",
            _ => "Tu membresía está al día. Gracias por tu visita."
        };

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

    private static string GetPaymentStatus(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            return "Tu membresía está al día";
        }

        if (identifier.Contains('@'))
        {
            return "Tu membresía está al día";
        }

        if (identifier.All(char.IsDigit))
        {
            return "Pago atrasado";
        }

        return "Próximo a vencer";
    }
}
