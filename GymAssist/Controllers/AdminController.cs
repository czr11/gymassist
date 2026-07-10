using GymAssist.Models;
using Microsoft.AspNetCore.Mvc;

namespace GymAssist.Controllers;

public class AdminController : Controller
{
    public IActionResult Login()
    {
        return View(new AdminLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Login(AdminLoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        TempData["AdminNotice"] = "La autenticación de administración estará disponible en la siguiente fase del proyecto.";
        return View(model);
    }
}
