namespace CelexWebApp.Models.AlumnoMMV
{
    public class HistorialAvanceAlumno
    {
        public int IdEstudiante { get; set; }
        public int IdProfesor { get; set; }
        public int IdCurso { get; set; }
        public decimal Asistencia { get; set; }
        public decimal CalContinua { get; set; }
        public decimal CalExMedia { get; set; }
        public decimal CalExFinal { get; set; }
    }
}
