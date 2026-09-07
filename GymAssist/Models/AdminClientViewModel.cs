using System.ComponentModel.DataAnnotations;

namespace GymAssist.Models;

public class AdminClientViewModel
{
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "Ingresa los nombres.")]
    [StringLength(100, ErrorMessage = "Los nombres no pueden superar los 100 caracteres.")]
    [Display(Name = "Nombres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa los apellidos.")]
    [StringLength(100, ErrorMessage = "Los apellidos no pueden superar los 100 caracteres.")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la cédula.")]
    [StringLength(20, ErrorMessage = "La cédula no puede superar los 20 caracteres.")]
    [Display(Name = "Cédula")]
    public string Cedula { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Ingresa un teléfono válido.")]
    [StringLength(20, ErrorMessage = "El teléfono no puede superar los 20 caracteres.")]
    [Display(Name = "Teléfono")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(100, ErrorMessage = "El correo no puede superar los 100 caracteres.")]
    [Display(Name = "Correo")]
    public string? Email { get; set; }

    [StringLength(500, ErrorMessage = "La dirección no puede superar los 500 caracteres.")]
    [Display(Name = "Dirección")]
    public string? Direccion { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de nacimiento")]
    public DateTime? FechaNacimiento { get; set; }

    [RegularExpression("M|F|O", ErrorMessage = "Selecciona un género válido.")]
    [Display(Name = "Género")]
    public string? Genero { get; set; }

    [Display(Name = "Activo")]
    public bool Activo { get; set; } = true;
}
