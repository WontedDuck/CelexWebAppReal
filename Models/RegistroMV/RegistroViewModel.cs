using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CelexWebApp.Models.RegistroMV
{
    public class RegistroViewModel
    {
        [Required(ErrorMessage = "¡Debes seleccionar un rol!")]
        public string RolSeleccionado { get; set; }
        public RegistroAlumnoViewModel Alumno { get; set; } = new RegistroAlumnoViewModel();
        public List<SelectListItem> Roles { get; set; }
    }
}