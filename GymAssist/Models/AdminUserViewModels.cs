using System.ComponentModel.DataAnnotations;

namespace GymAssist.Models;

public class AdminUserViewModel
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa el correo.")]
    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    [Display(Name = "Correo")]
    public string Email { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
    [Display(Name = "Contraseña")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "Selecciona un rol.")]
    [RegularExpression("admin|pagos", ErrorMessage = "El rol seleccionado no es válido.")]
    [Display(Name = "Rol")]
    public string Rol { get; set; } = "admin";

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
