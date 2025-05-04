using CelexWebApp.Controllers;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;

namespace CelexWebApp.Models.NotificacionesMMV
{
    public class NotificacionesModelView
    {
        List<NotificacionesModel> notificaciones = new List<NotificacionesModel>();
        public int id_profesor = 0;
        public int[] id_alumno;
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public NotificacionesModelView(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }
        public async Task<List<NotificacionesModel>> ObtenerNotificaciones(int id_registrado)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                // Consulta para obtener las notificaciones
                string query2 = "SELECT id_mensaje, contenido, fecha_registro, leido FROM Mensajes WHERE id_destinatario = @Id_destinatario ORDER BY leido ASC, fecha_registro DESC";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@Id_destinatario", id_registrado);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            NotificacionesModel notificacion = new NotificacionesModel
                            {
                                Id_Mensaje = reader.GetInt32(0),
                                Contenido = reader.GetString(1),
                                Fecha_Registro = reader.GetDateTime(2),
                                Leido = reader.GetBoolean(3)
                            };
                            notificaciones.Add(notificacion);
                        }
                    }
                }
            }
            return notificaciones ?? new List<NotificacionesModel>();
        }
    }
}
