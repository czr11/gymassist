namespace GymAssist.Models;

public class AdminPaymentListViewModel
{
    public int IdPago { get; set; }
    public string Cliente { get; set; } = string.Empty;
    public string Membresia { get; set; } = string.Empty;
    public string TipoPago { get; set; } = string.Empty;
    public decimal Monto { get; set; }
    public DateTime FechaPago { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string EstadoPago { get; set; } = string.Empty;
    public string EstadoVigencia { get; set; } = string.Empty;
}
