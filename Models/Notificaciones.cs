namespace CelexWebApp.Models
{
    public class Notificaciones
    {
        public int Id_Mensaje { get; set; }
        public string Id_Remitente { get; set; }
        public string Id_Destinatario { get; set; }
        public string Contenido { get; set; }
        public DateTime Fecha_Registro { get; set; }
        public bool Leido { get; set; }
    }
}
