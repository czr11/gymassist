using System.ComponentModel.DataAnnotations;

namespace GymAssist.Models;

public class AdminPaymentViewModel
{
    public int IdPago { get; set; }

    [Required(ErrorMessage = "Selecciona un cliente.")]
    [Display(Name = "Cliente")]
    public int IdCliente { get; set; }

    public int? IdMembresia { get; set; }

    [Required(ErrorMessage = "Selecciona el tipo de pago.")]
    [RegularExpression("matricula|mensualidad", ErrorMessage = "El tipo de pago no es válido.")]
    [Display(Name = "Tipo de pago")]
    public string TipoPago { get; set; } = "mensualidad";

    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "El monto debe ser mayor que cero.")]
    [Display(Name = "Monto")]
    public decimal Monto { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de vencimiento")]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(1);

    [Required(ErrorMessage = "Selecciona el método de pago.")]
    [RegularExpression("efectivo|tarjeta|transferencia|otro", ErrorMessage = "El método de pago no es válido.")]
    [Display(Name = "Método de pago")]
    public string MetodoPago { get; set; } = "efectivo";

    [StringLength(255, ErrorMessage = "El comprobante no puede superar los 255 caracteres.")]
    [Display(Name = "Comprobante")]
    public string? Comprobante { get; set; }

    [Required(ErrorMessage = "Selecciona el estado.")]
    [RegularExpression("pagado|pendiente|cancelado", ErrorMessage = "El estado no es válido.")]
    public string Estado { get; set; } = "pagado";

    [StringLength(1000, ErrorMessage = "Las observaciones no pueden superar los 1000 caracteres.")]
    public string? Observaciones { get; set; }
}
