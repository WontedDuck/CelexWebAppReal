using System.ComponentModel.DataAnnotations;

namespace CelexWebApp.Models.RegistroMV
{
    public class RegistroProfesorViewModel
    {
        [Required(ErrorMessage = "El número de empleado es obligatorio")]
        [StringLength(10, ErrorMessage = "Máximo 10 caracteres")]
        [MinLength(10, ErrorMessage = "Mínimo 10 dígitos")]
        [Display(Name = "Número de Empleado")]
        public string NumeroEmpleado { get; set; } = "";

        [Required(ErrorMessage = "El nombre completo es obligatorio")]
        [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
        [Display(Name = "Nombre Completo")]
        public string NombreCompleto { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(10, ErrorMessage = "Máximo 10 dígitos")]
        [MinLength(10, ErrorMessage = "Mínimo 10 dígitos")]
        [Phone(ErrorMessage = "Formato inválido")]
        public string Telefono { get; set; } = "";

        [Required(ErrorMessage = "Debe seleccionar un nivel")]
        [Display(Name = "Nivel que Imparte")]
        public string NivelImparte { get; set; } = "";
    }
}