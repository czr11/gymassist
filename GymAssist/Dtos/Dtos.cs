namespace GymAssist.Dtos;

public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }
}

public class ClienteDto
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
    public bool Activo { get; set; }
}

public class MembresiaDto
{
    public int IdMembresia { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public decimal Precio { get; set; }
    public int DuracionDias { get; set; }
    public bool AccesoSalas { get; set; }
    public bool AccesoEntrenador { get; set; }
    public bool AccesoSpa { get; set; }
    public bool Activo { get; set; }
}

public class PagoDto
{
    public int IdPago { get; set; }
    public int IdCliente { get; set; }
    public int IdMembresia { get; set; }
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? MetodoPago { get; set; }
    public string? Estado { get; set; }
}

public class CheckinDto
{
    public int IdCheckin { get; set; }
    public int IdCliente { get; set; }
    public DateTime FechaHoraEntrada { get; set; }
    public DateTime? FechaHoraSalida { get; set; }
    public string MetodoEntrada { get; set; } = string.Empty;
}

public class NotificacionDto
{
    public int IdNotificacion { get; set; }
    public int IdCliente { get; set; }
    public string? Tipo { get; set; }
    public string? Mensaje { get; set; }
    public DateTime FechaEnvio { get; set; }
    public bool Enviado { get; set; }
    public DateTime? FechaLectura { get; set; }
}
