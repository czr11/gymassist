using System.ComponentModel.DataAnnotations;

namespace GymAssist.Models;

public class AdminMembershipViewModel
{
    public int IdMembresia { get; set; }

    [Required(ErrorMessage = "Ingresa el nombre de la membresía.")]
    [StringLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string? Descripcion { get; set; }

    [Range(typeof(decimal), "0.01", "1000000", ErrorMessage = "El precio debe ser mayor que cero.")]
    [Display(Name = "Precio")]
    public decimal Precio { get; set; }

    [Range(1, 3650, ErrorMessage = "La duración debe estar entre 1 y 3650 días.")]
    [Display(Name = "Duración en días")]
    public int DuracionDias { get; set; } = 30;

    [Display(Name = "Acceso a salas")]
    public bool AccesoSalas { get; set; } = true;

    [Display(Name = "Acceso a entrenador")]
    public bool AccesoEntrenador { get; set; }

    [Display(Name = "Acceso a spa")]
    public bool AccesoSpa { get; set; }

    [Display(Name = "Activa")]
    public bool Activo { get; set; } = true;
}
