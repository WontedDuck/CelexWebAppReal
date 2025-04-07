using System.ComponentModel.DataAnnotations;

namespace CelexWebApp.Models.RegistroMV
{
    public class RegistroAlumnoViewModel
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(50, ErrorMessage = "Máximo 50 caracteres")]
        public string Nombre { get; set; } = "";

        [Required(ErrorMessage = "El apellido paterno es obligatorio")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
        [Display(Name = "Apellido Paterno")]
        public string ApellidoPaterno { get; set; } = "";

        [Display(Name = "Apellido Materno")]
        [StringLength(20, ErrorMessage = "Máximo 20 caracteres")]
        public string ApellidoMaterno { get; set; } = "";

        [Required(ErrorMessage = "El teléfono es obligatorio")]
        [StringLength(10, ErrorMessage = "Máximo 10 digitos")]
        [MinLength(10, ErrorMessage = "Minimo 10 digitos")]
        [Phone(ErrorMessage = "Formato inválido")]
        public string Telefono { get; set; } = "";

        [Required(ErrorMessage = "El número de boleta es obligatorio")]
        [StringLength(10, ErrorMessage = "Máximo 10 digitos")]
        [MinLength(10, ErrorMessage = "Minimo 10 digitos")]
        [Phone(ErrorMessage = "Formato inválido")]
        public string NumeroBoleta { get; set; } = "";

        [Required(ErrorMessage = "La matrícula es obligatoria")]
        [StringLength(30, ErrorMessage = "Máximo 30 caracteres")]
        public string Matricula { get; set; } = "";
    }
}