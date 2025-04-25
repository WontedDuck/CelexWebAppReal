using CelexWebApp.Models;
using CelexWebApp.Models.RegistroMV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using System.Diagnostics;
using Microsoft.Identity.Web;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CelexWebApp.Controllers
{
    [Authorize]
    public class RegistroController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public RegistroController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public IActionResult Index()
        {
            var model = new RegistroViewModel
            {
                Roles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Alumno", Text = "Alumno" }
                },
            };
            var user = _graphServiceClient.Me.GetAsync().Result;
            ViewData["id_azure"] = user.Id.ToString();
            ViewData["Title"] = "Registro";
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> EnviarID()
        {
            int id_registrado = 0;
            var user = await _graphServiceClient.Me.GetAsync();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Registrados (id_rol, id_azure) VALUES (@Id_rol, @Id_azure)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_rol", 4);
                    command.Parameters.AddWithValue("@Id_azure", user.Id.ToString());
                    await command.ExecuteNonQueryAsync();
                }
                string query2 = "SELECT id_registrado FROM Registrados WHERE id_azure = @id";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id", user.Id.ToString());
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            id_registrado = int.Parse(reader["id_registrado"].ToString());
                        }
                    }
                }
                int[] administradores;
                string query3 = "SELECT COUNT(*) FROM Registrados WHERE id_rol = 3";
                using (SqlCommand command = new SqlCommand(query3, connection))
                {
                    administradores = new int[(int)await command.ExecuteScalarAsync()];
                }
                string query4 = "SELECT id_registrado FROM Registrados WHERE id_rol = 3";
                using (SqlCommand command = new SqlCommand(query4, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        for(int i = 0; i < administradores.Length; i++)
                        {
                            administradores[i] = int.Parse(reader["id_registrado"].ToString());
                        }
                    }
                }
                string query5 = "INSERT INTO Mensajes (id_remitente, id_destinatario, contenido) VALUES (@Id_remitente, @Id_destinatario, @Contenido)";
                for (int i = 0; i < administradores.Length; i++)
                {
                    using (SqlCommand command = new SqlCommand(query5, connection))
                    {
                        command.Parameters.AddWithValue("@Id_remitente", id_registrado);
                        command.Parameters.AddWithValue("@Id_destinatario", administradores[i]);
                        command.Parameters.AddWithValue("@Contenido", $"Nuevo Profesor: {user.Id}");
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
            return RedirectToAction("Index", "Home");
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]

        [HttpGet]
        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Registro(RegistroViewModel model)
        {
            model.Roles = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Alumno", Text = "Alumno" },
                };
            int id_registrado = 0;
            var user = await _graphServiceClient.Me.GetAsync();
            using (var connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {

                await connection.OpenAsync();
                string queryRegistro = "INSERT INTO Registrados (id_rol, id_azure) VALUES (@Id_rol, @Id_azure)";
                using (SqlCommand command = new SqlCommand(queryRegistro, connection))
                {
                    command.Parameters.AddWithValue("@Id_rol", 1);
                    command.Parameters.AddWithValue("@Id_azure", user.Id.ToString());
                    command.ExecuteNonQuery();
                }
                string queryId = "SELECT id_registrado FROM Registrados WHERE id_azure = @id";
                using (SqlCommand command = new SqlCommand(queryId, connection))
                {
                    command.Parameters.AddWithValue("@id", user.Id.ToString());
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            id_registrado = int.Parse(reader["id_registrado"].ToString());
                        }
                    }
                }
                string queryAlumno = @"INSERT INTO Alumnos 
                        (nombre_alumno, apellido_paterno, apellido_materno, telefono_alumno, matricula, id_registrado) 
                        VALUES (@Nombre, @ApellidoPaterno, @ApellidoMaterno, @Telefono, @Matricula, @IdAzure)";
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        using (SqlCommand command = new SqlCommand(queryAlumno, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Nombre", model.Alumno.Nombre.ToString());
                            command.Parameters.AddWithValue("@ApellidoPaterno", model.Alumno.ApellidoPaterno.ToString());
                            command.Parameters.AddWithValue("@ApellidoMaterno", model.Alumno.ApellidoMaterno.ToString());
                            command.Parameters.AddWithValue("@Telefono", long.Parse(model.Alumno.Telefono.ToString()));
                            command.Parameters.AddWithValue("@Matricula", model.Alumno.Matricula.ToString());
                            command.Parameters.AddWithValue("@IdAzure", id_registrado);
                            command.ExecuteNonQuery();
                        }
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
