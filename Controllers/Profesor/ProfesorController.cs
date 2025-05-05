using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.NotificacionesMMV;
using CelexWebApp.Models.ProfesorMMV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;

namespace CelexWebApp.Controllers.Profesor
{
    public class ProfesorController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public ProfesorController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Index()
        {
            List<NotificacionesModel> notificaciones = new List<NotificacionesModel>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query2 = "SELECT id_mensaje, contenido, fecha_registro, leido FROM Mensajes WHERE id_destinatario = @Id_destinatario ORDER BY leido ASC, fecha_registro DESC";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@Id_destinatario", int.Parse(HttpContext.Session.GetString("id_registrado")));
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
            ViewData["Notificaciones"] = notificaciones;
            var user = await _graphServiceClient.Me.GetAsync();
            ProfesorModel profesor = null;
            List<GrupoModel> grupos = new List<GrupoModel>();
            TipoGrupoModel tipoGrupo = new TipoGrupoModel();

            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string queryProfesor = "SELECT * FROM Profesores WHERE id_registrado = @id";
                using (SqlCommand command = new SqlCommand(queryProfesor, connection))
                {
                    command.Parameters.AddWithValue("@id", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    {

                    }
                    if (reader.Read())
                    {
                        profesor = new ProfesorModel
                        {
                            Id = int.Parse(reader["id_profesor"].ToString()),
                            Nombre = reader["nombre_profesor"].ToString(),
                            Telefono = reader["telefono_profesor"].ToString(),
                        };
                        HttpContext.Session.SetString("id", profesor.Id.ToString());
                        HttpContext.Session.SetString("nombre", profesor.Nombre);
                        HttpContext.Session.SetString("telefono", profesor.Telefono);
                    }
                    reader.Close();
                }
                string queryGrupos = "SELECT DISTINCT c.id_cursos, c.nombre_curso, c.id_nivel FROM Avance_Alumnos a JOIN Curso c ON a.id_cursos = c.id_cursos WHERE a.id_profesor = @idProfesor;";
                using (SqlCommand command = new SqlCommand(queryGrupos, connection))
                {
                    command.Parameters.AddWithValue("@idProfesor", int.Parse(HttpContext.Session.GetString("id")));
                    SqlDataReader reader2 = await command.ExecuteReaderAsync();
                    while (reader2.Read())
                    {
                        GrupoModel grupo = new GrupoModel
                        {
                            Id = int.Parse(reader2["id_cursos"].ToString()),
                            Nombre = reader2["nombre_curso"].ToString(),
                            Nivel = tipoGrupo.Niveles(reader2["id_nivel"].ToString()),
                        };
                        grupos.Add(grupo);
                    }
                    reader2.Close();
                }
            }
            ProfesorViewModel profesorViewModel = new ProfesorViewModel
            {
                Profesor = profesor,
                Grupos = grupos
            };
            return View(profesorViewModel);
        }
        [HttpPost]
        public async Task<IActionResult> EnviarMensaje(string destinatarios, string mensaje)
        {
            int rol = 0;
            string query = "SELECT id_registrado FROM Registrados WHERE id_rol = @rol";
            switch (destinatarios)
            {
                case "Alumnos":
                    rol = 1;
                    break;
                case "Administradores":
                    rol = 3;
                    break;
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    await connection.OpenAsync();
                    List<int> ndestinatarios = new List<int>();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@rol", rol);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ndestinatarios.Add(Convert.ToInt32(reader["id_registrado"]));
                            }
                        }
                    }
                    foreach (int idDestinatario in ndestinatarios)
                    {
                        string query2 = "INSERT INTO Mensajes (id_remitente, id_destinatario, contenido) VALUES (@Id_remitente, @Id_destinatario, @Contenido)";
                        using (SqlCommand command2 = new SqlCommand(query2, connection))
                        {
                            command2.Parameters.AddWithValue("@Id_remitente", int.Parse(HttpContext.Session.GetString("id_registrado")));
                            command2.Parameters.AddWithValue("@Id_destinatario", idDestinatario);
                            command2.Parameters.AddWithValue("@Contenido", $"Mensaje de Administrador: {mensaje}");
                            await command2.ExecuteNonQueryAsync();
                        }
                    }
                }
                TempData["MensajeEstado"] = "El mensaje fue enviado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["MensajeEstado"] = $"Error al enviar el mensaje, enviar de nuevo: {ex.Message}";

                return RedirectToAction("Index");
            }
        }
        public IActionResult MarcarComoLeida(string id_mensaje)
        {
            using (SqlConnection connection = new SqlConnection(_conexion.GetConexionAsync().Result))
            {
                connection.Open();
                string query = "UPDATE Mensajes SET leido = 1 WHERE id_mensaje = @Id_mensaje";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_mensaje", id_mensaje);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
        public IActionResult BorrarNotificacion(string id_mensaje)
        {
            using (SqlConnection connection = new SqlConnection(_conexion.GetConexionAsync().Result))
            {
                connection.Open();
                string query = "DELETE FROM Mensajes WHERE id_mensaje = @Id_mensaje";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_mensaje", id_mensaje);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
    }
}
