using System.ComponentModel.DataAnnotations;

namespace GymAssist.Models;

public class CheckInViewModel
{
    [Required(ErrorMessage = "Ingresa tu correo, ID o teléfono.")]
    [Display(Name = "Correo, ID o teléfono")]
    public string Identificador { get; set; } = string.Empty;

    [Required(ErrorMessage = "Selecciona un gimnasio.")]
    [Display(Name = "Gimnasio")]
    public string GymName { get; set; } = "Alphamma.sv";
}
