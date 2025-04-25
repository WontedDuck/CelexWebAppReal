namespace CelexWebApp.Models.AdministradorMMV
{
    public class GrupoModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Nivel { get; set; }
        public string TipoCurso { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int capacidad { get; set; }
    }
}
