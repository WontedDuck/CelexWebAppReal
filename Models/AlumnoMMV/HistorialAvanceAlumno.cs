namespace CelexWebApp.Models.AlumnoMMV
{
    public class HistorialAvanceAlumno
    {
        public int IdEstudiante { get; set; }
        public string Modulo { get; set; }
        public string Nombre { get; set; }
        public string Boleta { get; set; }
        public int IdProfesor { get; set; }
        public string NombreProfesor { get; set; }
        public int IdCurso { get; set; }
        public string NombreCurso { get; set; }
        public decimal Asistencia { get; set; }
        public decimal CalContinua { get; set; }
        public decimal CalExMedia { get; set; }
        public decimal CalExFinal { get; set; }
        public decimal CalificacionTotal { get; set; }
        public string NivelMCER { get; set; }
        public string Periodo { get; set; }
    }
}
