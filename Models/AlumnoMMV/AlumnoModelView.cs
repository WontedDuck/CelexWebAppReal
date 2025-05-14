namespace CelexWebApp.Models.AlumnoMMV
{
    public class AlumnoModelView
    {
        public AlumnoModel alumno { get; set; } = new AlumnoModel(); 
        public float CalContinua { get; set; }
        public float CalExMedia { get; set; }
        public float CaliExFinal { get; set; }
    }
}
