namespace GymAssist.Models;

public class PagoGestion
{
    public int IdPago { get; set; }
    public int IdCliente { get; set; }
    public int IdMembresia { get; set; }
    public int? IdUsuarioRegistro { get; set; }
    public decimal Monto { get; set; }
    public string TipoPago { get; set; } = string.Empty;
    public DateTime FechaPago { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? MetodoPago { get; set; }
    public string? Comprobante { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public string? Observaciones { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public bool ClienteActivo { get; set; }
    public string Membresia { get; set; } = string.Empty;
    public string EstadoVigencia { get; set; } = string.Empty;
}
