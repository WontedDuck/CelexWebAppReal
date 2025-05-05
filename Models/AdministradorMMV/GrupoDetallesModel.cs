using CelexWebApp.Models.AlumnoMMV;
using Microsoft.Graph.Models;

namespace CelexWebApp.Models.AdministradorMMV
{
    public class GrupoDetallesModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Nivel { get; set; }
        public string TipoCurso { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int Capacidad { get; set; }
        public int Ocupados { get; set; } = 0;
        public DateTime FechaCreacion { get; set; }
        public string Profesor { get; set; }

        public List<AlumnoModelView> Alumnos { get; set; } = new List<AlumnoModelView>();


    }
}
