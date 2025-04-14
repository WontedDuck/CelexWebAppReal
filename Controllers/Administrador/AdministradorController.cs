using CelexWebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CelexWebApp.Controllers.Administrador
{
    public class AdministradorController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public AdministradorController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
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
            var user = await _graphServiceClient.Me.GetAsync();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                connection.Open();
                string query = "SELECT * FROM Administrador WHERE id_registrado = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        //Guardar la informacion del administrador de manera local para su uso durante todo el proyecto
                        HttpContext.Session.SetString("id", reader["id_administrador"].ToString());
                        HttpContext.Session.SetString("nombre", reader["nombre"].ToString());
                        HttpContext.Session.SetString("telefono", reader["telefono"].ToString());
                    }
                }
            }
            return View();
        }

        [HttpPost]

        public async Task<IActionResult> EnviarNotificacion(string mensaje, string destinatarios)
        {
            int rol = 0;
            string query = "SELECT id_registrado FROM Registrados WHERE id_rol = @rol";
            switch (destinatarios)
            {
                case "Todos":
                    query = "SELECT id_registrado FROM Registrados WHERE id_rol != @rol";
                    rol = 3;
                    break;
                case "Alumnos":
                    rol = 1;
                    break;
                case "Profesores":
                    rol = 2;
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

        [HttpPost]
        public IActionResult AgregarProfesor(string nombreProfesor, string telefonoProfesor, string nivelProfesor)
        {
            // Lógica para agregar un profesor
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult GenerarHistorial(int alumnoSeleccionado)
        {
            // Lógica para generar el historial del alumno seleccionado
            return RedirectToAction("Index");
        }

        [HttpPost]
        public IActionResult CrearGrupo(string nombreGrupo, string nivelGrupo)
        {
            // Lógica para crear un grupo
            return RedirectToAction("Index");
        }
    }
}
