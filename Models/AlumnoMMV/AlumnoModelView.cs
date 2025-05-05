namespace CelexWebApp.Models.AlumnoMMV
{
    public class AlumnoModelView
    {
        public AlumnoModel alumno { get; set; } = new AlumnoModel(); 
        public int CalContinua { get; set; }
        public int CalExMedia { get; set; }
        public int CaliExFinal { get; set; }
    }
}
