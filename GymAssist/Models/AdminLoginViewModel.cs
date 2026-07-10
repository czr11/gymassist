using System.ComponentModel.DataAnnotations;

namespace GymAssist.Models;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Ingresa tu usuario.")]
    [Display(Name = "Usuario")]
    public string Usuario { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa tu contraseña.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña")]
    public string Password { get; set; } = string.Empty;
}
