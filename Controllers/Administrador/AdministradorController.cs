﻿using CelexWebApp.Models;
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
            // Al iniciar la aplicacion revisar si le han llegado notificaciones al administrador
            List<string> notificaciones = new List<string>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                // Consulta para obtener las notificaciones del administrador
                string query = "SELECT contenido, fecha_registro FROM Mensajes WHERE id_destinatario = @Id_destinatario ORDER BY fecha_registro DESC";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_destinatario", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            string contenido = reader["contenido"].ToString();
                            DateTime fecha = Convert.ToDateTime(reader["fecha_registro"]);
                            notificaciones.Add($"{contenido} - {fecha:dd/MM/yyyy}");
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
        public async Task<IActionResult> AgregarProfesor(int numeroEmpleado, string nombreProfesor, string telefonoProfesor, string idAzure)
        {
            int idAzureInt = 0;
            //Buscar que el profesor no exista en la base de datos
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
            //Encontrar el id_registrado del profesor en la base de datos con su id_azure
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
            // Lógica para agregar un profesor
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
            //Actualizar el rol del profesor en la base de datos
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
            // Enviar notificación al profesor
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
            // Enviar mensaje de que salio skibidibien
            TempData["MensajeEstadoAgregar"] = $"El profesor {nombreProfesor} se a agregado exitosamente";
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
