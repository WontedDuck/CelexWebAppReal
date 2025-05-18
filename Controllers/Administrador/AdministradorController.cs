using CelexWebApp.Models;
using CelexWebApp.Models.NotificacionesMMV;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> AgregarProfesor(int numeroEmpleado, string nombreProfesor, string telefonoProfesor, string idAzure)
        {
            int idAzureInt = 0;
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM Profesores WHERE numero_empleado = @NumeroEmpleado";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumeroEmpleado", numeroEmpleado);
                    int count = (int)await command.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        TempData["MensajeEstadoAgregar"] = "El profesor ya existe en la base de datos.";
                        return RedirectToAction("Index");
                    }
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT id_registrado FROM Registrados WHERE id_azure = @IdAzure";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdAzure", idAzure);
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        idAzureInt = Convert.ToInt32(reader["id_registrado"]);
                    }
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Profesores (numero_empleado, nombre_profesor, telefono_profesor, id_registrado) VALUES (@NumeroEmpleado, @NombreProfesor, @TelefonoProfesor, @IdAzure)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumeroEmpleado", numeroEmpleado);
                    command.Parameters.AddWithValue("@NombreProfesor", nombreProfesor);
                    command.Parameters.AddWithValue("@TelefonoProfesor", telefonoProfesor);
                    command.Parameters.AddWithValue("@IdAzure", idAzureInt);
                    command.ExecuteNonQuery();
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "UPDATE Registrados SET id_rol = 2 WHERE id_registrado = @IdAzure";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdAzure", idAzureInt);
                    command.ExecuteNonQuery();
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Mensajes (id_remitente, id_destinatario, contenido) VALUES (@Id_remitente, @Id_destinatario, @Contenido)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_remitente", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    command.Parameters.AddWithValue("@Id_destinatario", idAzureInt);
                    command.Parameters.AddWithValue("@Contenido", $"Hola {nombreProfesor}, has sido agregado como profesor.");
                    await command.ExecuteNonQueryAsync();
                }
            }
            TempData["MensajeEstadoAgregar"] = $"El profesor {nombreProfesor} se a agregado exitosamente";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CrearGrupo(string nombreGrupo, string nivelGrupo, string tipoCurso, DateTime fechaInicio, DateTime fechaFin, int cantidadEstudiantes)
        {
            try
            {
                int idnivel, idtipo;
                switch(nivelGrupo)
                {
                    case "Introductorio":
                        idnivel = 1; break;
                    case "Basico1":
                        idnivel = 2; break;
                    case "Basico2":
                        idnivel = 3; break;
                    case "Basico3":
                        idnivel = 4; break;
                    case "Basico4":
                        idnivel = 5; break;
                    case "Basico5":
                        idnivel = 6; break;
                    case "Intermedio1":
                        idnivel = 7; break;
                    case "Intermedio2":
                        idnivel = 8; break;
                    case "Intermedio3":
                        idnivel = 9; break;
                    case "Intermedio4":
                        idnivel = 10; break;
                    case "Intermedio5":
                        idnivel = 11; break;
                    case "Avanzado1":
                        idnivel = 12; break;
                    case "Avanzado2":
                        idnivel = 13; break;
                    case "Avanzado3":
                        idnivel = 14; break;
                    case "Avanzado4":
                        idnivel = 15; break;
                    case "Avanzado5":
                        idnivel = 16; break;
                    case "FCE":
                        idnivel = 17; break;
                    default:
                        TempData["MensajeEstadoCrearGrupo"] = "Nivel de Curso no seleccionado";
                        return RedirectToAction("Index");
                }
                switch (tipoCurso)
                {
                    case "Semanal":
                        idtipo = 1; break;
                    case "Sabatino":
                        idtipo = 2; break;
                    case "Intensivo":
                        idtipo = 3; break;
                    default:
                        TempData["MensajeEstadoCrearGrupo"] = "Tipo de Curso no seleccionado";
                        return RedirectToAction("Index");
                }
                if (fechaInicio >= fechaFin)
                {
                    TempData["MensajeEstadoCrearGrupo"] = "La fecha de inicio debe ser anterior a la fecha de fin.";
                    return RedirectToAction("Index");
                }
                using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    await connection.OpenAsync();
                    string query = @"
                INSERT INTO Curso (nombre_curso, id_nivel, id_tipo_curso, fecha_inicio, fecha_fin, capacidad) 
                VALUES (@NombreCurso, @Id_Nivel, @Id_TipoCurso, @FechaInicio, @FechaFin, @Capacidad)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NombreCurso", nombreGrupo);
                        command.Parameters.AddWithValue("@Id_Nivel", idnivel);
                        command.Parameters.AddWithValue("@Id_TipoCurso", idtipo);
                        command.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                        command.Parameters.AddWithValue("@FechaFin", fechaFin);
                        command.Parameters.AddWithValue("@Capacidad", cantidadEstudiantes);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["MensajeEstadoCrearGrupo"] = "El grupo se creó exitosamente.";
                return RedirectToAction("Index");
            }   
            catch (Exception ex)
            {
                TempData["MensajeEstadoCrearGrupo"] = $"Error al crear el grupo: {ex.Message}";
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
