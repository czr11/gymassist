namespace GymAssist.Models;

public class Usuario
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Rol { get; set; } = "admin";
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; }
    public DateTime? UltimoAcceso { get; set; }
}

public class Cliente
{
    public int IdCliente { get; set; }
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Cedula { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public DateTime? FechaNacimiento { get; set; }
    public char? Genero { get; set; }
    public DateTime FechaRegistro { get; set; }
    public bool Activo { get; set; } = true;
    public int? IdUsuarioRegistro { get; set; }
}

public class Membresia
{
    public int IdMembresia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionDias { get; set; } = 30;
    public bool AccesoSalas { get; set; } = true;
    public bool AccesoEntrenador { get; set; }
    public bool AccesoSpa { get; set; }
    public bool Activo { get; set; } = true;
}

public class Pago
{
    public int IdPago { get; set; }
    public int IdCliente { get; set; }
    public int IdMembresia { get; set; }
    public int? IdUsuarioRegistro { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? MetodoPago { get; set; }
    public string? Comprobante { get; set; }
    public string Estado { get; set; } = "pagado";
    public string? Observaciones { get; set; }
}

public class Checkin
{
    public int IdCheckin { get; set; }
    public int IdCliente { get; set; }
    public DateTime FechaHoraEntrada { get; set; }
    public DateTime? FechaHoraSalida { get; set; }
    public string MetodoEntrada { get; set; } = "self";
    public int? IdUsuarioRegistro { get; set; }
}

public class Notificacion
{
    public int IdNotificacion { get; set; }
    public int IdCliente { get; set; }
    public string? Tipo { get; set; }
    public string? Mensaje { get; set; }
    public DateTime FechaEnvio { get; set; }
    public bool Enviado { get; set; }
    public DateTime? FechaLectura { get; set; }
}
