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
    public async Task<IActionResult> Clientes(string estado = "todos")
    {
        estado = estado.ToLowerInvariant() switch
        {
            "activos" => "activos",
            "inactivos" => "inactivos",
            _ => "todos"
        };

        var clientesQuery = dbContext.Clientes.AsNoTracking();
        if (estado == "activos")
        {
            clientesQuery = clientesQuery.Where(cliente => cliente.Activo);
        }
        else if (estado == "inactivos")
        {
            clientesQuery = clientesQuery.Where(cliente => !cliente.Activo);
        }

        var clientes = await clientesQuery
            .OrderBy(cliente => cliente.Apellidos)
            .ThenBy(cliente => cliente.Nombres)
            .ToListAsync();

        ViewData["ClientStatusFilter"] = estado;

        return View(clientes);
    }

    [Authorize(Roles = "admin,super_admin")]
    public IActionResult CrearCliente()
    {
        ViewBag.Membresias = dbContext.Membresias
            .Where(membresia => membresia.Activo)
            .OrderBy(membresia => membresia.Nombre)
            .ToList();
        return View(new AdminClientViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> CrearCliente(AdminClientViewModel model)
    {
        var membresia = model.IdMembresia.HasValue
            ? await dbContext.Membresias.FirstOrDefaultAsync(candidate => candidate.IdMembresia == model.IdMembresia && candidate.Activo)
            : null;
        if (membresia is null)
        {
            ModelState.AddModelError(nameof(model.IdMembresia), "Selecciona una membresía activa.");
        }

        var cedula = model.Cedula.Trim();
        if (await dbContext.Clientes.AnyAsync(cliente => cliente.Cedula == cedula))
        {
            ModelState.AddModelError(nameof(model.Cedula), "Ya existe un cliente con esa cédula.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Membresias = await dbContext.Membresias
                .Where(candidate => candidate.Activo)
                .OrderBy(candidate => candidate.Nombre)
                .ToListAsync();
            return View(model);
        }

        var cliente = new Cliente
        {
            Nombres = model.Nombres.Trim(),
            Apellidos = model.Apellidos.Trim(),
            Cedula = cedula,
            Telefono = CleanOptional(model.Telefono),
            Email = CleanOptional(model.Email)?.ToLowerInvariant(),
            Direccion = CleanOptional(model.Direccion),
            FechaNacimiento = model.FechaNacimiento,
            Genero = ParseGenero(model.Genero),
            FechaRegistro = DateTime.UtcNow,
            Activo = model.Activo,
            IdUsuarioRegistro = GetCurrentUserId()
        };
        dbContext.Clientes.Add(cliente);
        await dbContext.SaveChangesAsync();

        dbContext.Pagos.Add(new Pago
        {
            IdCliente = cliente.IdCliente,
            IdMembresia = membresia!.IdMembresia,
            IdUsuarioRegistro = GetCurrentUserId(),
            Monto = membresia.Precio,
            TipoPago = "matricula",
            FechaPago = DateTime.UtcNow,
            FechaInicio = DateTime.Today,
            FechaFin = DateTime.Today.AddDays(membresia.DuracionDias),
            MetodoPago = "efectivo",
            Estado = "pagado",
            Observaciones = "Matrícula generada al registrar el cliente."
        });
        await dbContext.SaveChangesAsync();

        TempData["AdminNotice"] = "Cliente creado correctamente.";
        return RedirectToAction(nameof(Clientes));
    }

    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EditarCliente(int id)
    {
        var cliente = await dbContext.Clientes.FindAsync(id);
        if (cliente is null)
        {
            return NotFound();
        }

        var membresiaActual = await dbContext.Pagos
            .Where(pago => pago.IdCliente == id && pago.Estado != "cancelado")
            .OrderByDescending(pago => pago.FechaPago)
            .FirstOrDefaultAsync();
        ViewBag.Membresias = await dbContext.Membresias
            .Where(membresia => membresia.Activo)
            .OrderBy(membresia => membresia.Nombre)
            .ToListAsync();

        return View(new AdminClientViewModel
        {
            IdCliente = cliente.IdCliente,
            Nombres = cliente.Nombres,
            Apellidos = cliente.Apellidos,
            Cedula = cliente.Cedula,
            Telefono = cliente.Telefono,
            Email = cliente.Email,
            Direccion = cliente.Direccion,
            FechaNacimiento = cliente.FechaNacimiento,
            Genero = cliente.Genero?.ToString(),
            Activo = cliente.Activo,
            IdMembresia = membresiaActual?.IdMembresia
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EditarCliente(AdminClientViewModel model)
    {
        var cliente = await dbContext.Clientes.FindAsync(model.IdCliente);
        if (cliente is null)
        {
            return NotFound();
        }

        var membresia = model.IdMembresia.HasValue
            ? await dbContext.Membresias.FirstOrDefaultAsync(candidate => candidate.IdMembresia == model.IdMembresia && candidate.Activo)
            : null;
        if (membresia is null)
        {
            ModelState.AddModelError(nameof(model.IdMembresia), "Selecciona una membresía activa.");
        }

        var cedula = model.Cedula.Trim();
        if (await dbContext.Clientes.AnyAsync(candidate =>
                candidate.IdCliente != model.IdCliente && candidate.Cedula == cedula))
        {
            ModelState.AddModelError(nameof(model.Cedula), "Ya existe un cliente con esa cédula.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Membresias = await dbContext.Membresias
                .Where(candidate => candidate.Activo)
                .OrderBy(candidate => candidate.Nombre)
                .ToListAsync();
            return View(model);
        }

        cliente.Nombres = model.Nombres.Trim();
        cliente.Apellidos = model.Apellidos.Trim();
        cliente.Cedula = cedula;
        cliente.Telefono = CleanOptional(model.Telefono);
        cliente.Email = CleanOptional(model.Email)?.ToLowerInvariant();
        cliente.Direccion = CleanOptional(model.Direccion);
        cliente.FechaNacimiento = model.FechaNacimiento;
        cliente.Genero = ParseGenero(model.Genero);
        cliente.Activo = model.Activo;

        await dbContext.SaveChangesAsync();

        var membresiaActual = await dbContext.Pagos
            .Where(pago => pago.IdCliente == cliente.IdCliente && pago.Estado != "cancelado")
            .OrderByDescending(pago => pago.FechaPago)
            .FirstOrDefaultAsync();
        if (membresiaActual?.IdMembresia != membresia!.IdMembresia)
        {
            dbContext.Pagos.Add(new Pago
            {
                IdCliente = cliente.IdCliente,
                IdMembresia = membresia.IdMembresia,
                IdUsuarioRegistro = GetCurrentUserId(),
                Monto = membresia.Precio,
                TipoPago = "matricula",
                FechaPago = DateTime.UtcNow,
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today.AddDays(membresia.DuracionDias),
                MetodoPago = "efectivo",
                Estado = "pagado",
                Observaciones = "Matrícula generada al cambiar la membresía del cliente."
            });
            await dbContext.SaveChangesAsync();
        }
        TempData["AdminNotice"] = "Cliente actualizado correctamente.";
        return RedirectToAction(nameof(Clientes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EliminarCliente(int id)
    {
        var cliente = await dbContext.Clientes.FindAsync(id);
        if (cliente is null)
        {
            return NotFound();
        }

        cliente.Activo = false;
        await dbContext.SaveChangesAsync();
        TempData["AdminNotice"] = "Cliente desactivado correctamente.";
        return RedirectToAction(nameof(Clientes));
    }

    private int? GetCurrentUserId()
    {
        return int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
    }

    private static string? CleanOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static char? ParseGenero(string? genero)
    {
        return string.IsNullOrWhiteSpace(genero) ? null : genero[0];
    }

    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> Pagos(string? estado = null, string? tipo = null, string? buscar = null)
    {
        var query = from pago in dbContext.Pagos.AsNoTracking()
                    join cliente in dbContext.Clientes.AsNoTracking() on pago.IdCliente equals cliente.IdCliente
                    join membresia in dbContext.Membresias.AsNoTracking() on pago.IdMembresia equals membresia.IdMembresia
                    select new { pago, cliente, membresia };

        buscar = buscar?.Trim();
        if (!string.IsNullOrWhiteSpace(buscar))
        {
            var pattern = $"%{buscar}%";
            var codigoCliente = int.TryParse(buscar, out var parsedCode) ? parsedCode : (int?)null;
            query = query.Where(item =>
                (codigoCliente.HasValue && item.cliente.IdCliente == codigoCliente.Value) ||
                EF.Functions.ILike(item.cliente.Nombres + " " + item.cliente.Apellidos, pattern) ||
                EF.Functions.ILike(item.cliente.Email ?? string.Empty, pattern) ||
                EF.Functions.ILike(item.cliente.Cedula, pattern) ||
                EF.Functions.ILike(item.cliente.Telefono ?? string.Empty, pattern));
        }

        if (estado == "vencido")
        {
            query = query.Where(item => item.pago.Estado != "cancelado" && item.pago.FechaFin < DateTime.UtcNow.Date);
        }
        else if (estado is "pagado" or "pendiente" or "cancelado")
        {
            query = query.Where(item => item.pago.Estado == estado &&
                (estado == "cancelado" || item.pago.FechaFin >= DateTime.UtcNow.Date));
        }
        else
        {
            estado = "todos";
        }

        if (tipo is "matricula" or "mensualidad")
        {
            query = query.Where(item => item.pago.TipoPago == tipo);
        }
        else
        {
            tipo = "todos";
        }

        ViewData["PaymentStatusFilter"] = estado;
        ViewData["PaymentTypeFilter"] = tipo;
        ViewData["PaymentSearch"] = buscar ?? string.Empty;
        var pagos = await query
            .OrderByDescending(item => item.pago.FechaPago)
            .Select(item => new AdminPaymentListViewModel
            {
                IdPago = item.pago.IdPago,
                Cliente = item.cliente.Nombres + " " + item.cliente.Apellidos,
                Membresia = item.membresia.Nombre,
                TipoPago = item.pago.TipoPago,
                Monto = item.pago.Monto,
                FechaPago = item.pago.FechaPago,
                FechaInicio = item.pago.FechaInicio,
                FechaFin = item.pago.FechaFin,
                Estado = item.pago.Estado != "cancelado" && item.pago.FechaFin < DateTime.UtcNow.Date
                    ? "vencido"
                    : item.pago.Estado
            })
            .ToListAsync();
        return View(pagos);
    }

    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> CrearPago()
    {
        await LoadPaymentOptions();
        return View(new AdminPaymentViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> CrearPago(AdminPaymentViewModel model)
    {
        var membresia = await GetCurrentMembershipAsync(model.IdCliente);
        if (membresia is null)
        {
            ModelState.AddModelError(nameof(model.IdCliente), "El cliente no tiene una membresía activa asignada.");
        }
        if (model.FechaFin < model.FechaInicio)
        {
            ModelState.AddModelError(nameof(model.FechaFin), "La fecha de vencimiento no puede ser anterior al inicio.");
        }
        if (!ModelState.IsValid)
        {
            await LoadPaymentOptions();
            return View(model);
        }

        dbContext.Pagos.Add(new Pago
        {
            IdCliente = model.IdCliente, IdMembresia = membresia!.IdMembresia, IdUsuarioRegistro = GetCurrentUserId(),
            Monto = model.Monto, TipoPago = model.TipoPago, FechaPago = model.FechaPago,
            FechaInicio = model.FechaInicio, FechaFin = model.FechaFin, MetodoPago = model.MetodoPago,
            Comprobante = CleanOptional(model.Comprobante), Estado = model.Estado,
            Observaciones = CleanOptional(model.Observaciones)
        });
        await dbContext.SaveChangesAsync();
        TempData["AdminNotice"] = "Pago registrado correctamente.";
        return RedirectToAction(nameof(Pagos));
    }

    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EditarPago(int id)
    {
        var pago = await dbContext.Pagos.FindAsync(id);
        if (pago is null) return NotFound();
        await LoadPaymentOptions();
        return View(ToPaymentViewModel(pago));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EditarPago(AdminPaymentViewModel model)
    {
        var pago = await dbContext.Pagos.FindAsync(model.IdPago);
        if (pago is null) return NotFound();
        if (model.FechaFin < model.FechaInicio)
        {
            ModelState.AddModelError(nameof(model.FechaFin), "La fecha de vencimiento no puede ser anterior al inicio.");
        }
        if (!ModelState.IsValid)
        {
            await LoadPaymentOptions();
            return View(model);
        }

        pago.IdCliente = model.IdCliente; pago.Monto = model.Monto;
        pago.TipoPago = model.TipoPago; pago.FechaPago = model.FechaPago; pago.FechaInicio = model.FechaInicio;
        pago.FechaFin = model.FechaFin; pago.MetodoPago = model.MetodoPago;
        pago.Comprobante = CleanOptional(model.Comprobante); pago.Estado = model.Estado;
        pago.Observaciones = CleanOptional(model.Observaciones);
        await dbContext.SaveChangesAsync();
        TempData["AdminNotice"] = "Pago actualizado correctamente.";
        return RedirectToAction(nameof(Pagos));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "admin,super_admin")]
    public async Task<IActionResult> EliminarPago(int id)
    {
        var pago = await dbContext.Pagos.FindAsync(id);
        if (pago is null) return NotFound();
        pago.Estado = "cancelado";
        await dbContext.SaveChangesAsync();
        TempData["AdminNotice"] = "Pago cancelado correctamente.";
        return RedirectToAction(nameof(Pagos));
    }

    private async Task LoadPaymentOptions()
    {
        ViewBag.Clientes = await dbContext.Clientes.Where(cliente => cliente.Activo).OrderBy(cliente => cliente.Apellidos).ToListAsync();
        var membershipPrices = await (from pago in dbContext.Pagos.AsNoTracking()
                                      join membresia in dbContext.Membresias.AsNoTracking() on pago.IdMembresia equals membresia.IdMembresia
                                      where pago.Estado != "cancelado" && membresia.Activo
                                      select new { pago.IdCliente, pago.FechaPago, membresia.Precio })
            .ToListAsync();
        ViewBag.MembershipPrices = membershipPrices
            .GroupBy(item => item.IdCliente)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.FechaPago).First().Precio);
    }

    private async Task<Membresia?> GetCurrentMembershipAsync(int idCliente)
    {
        return await (from pago in dbContext.Pagos
                      join membresia in dbContext.Membresias on pago.IdMembresia equals membresia.IdMembresia
                      where pago.IdCliente == idCliente && pago.Estado != "cancelado" && membresia.Activo
                      orderby pago.FechaPago descending
                      select membresia).FirstOrDefaultAsync();
    }

    private static AdminPaymentViewModel ToPaymentViewModel(Pago pago) => new()
    {
        IdPago = pago.IdPago, IdCliente = pago.IdCliente, IdMembresia = pago.IdMembresia,
        TipoPago = pago.TipoPago, Monto = pago.Monto, FechaPago = pago.FechaPago,
        FechaInicio = pago.FechaInicio, FechaFin = pago.FechaFin, MetodoPago = pago.MetodoPago ?? "efectivo",
        Comprobante = pago.Comprobante, Estado = pago.Estado, Observaciones = pago.Observaciones
    };

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
