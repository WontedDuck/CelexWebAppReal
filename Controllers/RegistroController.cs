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
                    new SelectListItem { Value = "Alumno", Text = "Alumno" },
                    new SelectListItem { Value = "Profesor", Text = "Profesor" }
                },
                Niveles = ObtenerNiveles()
            };
            ViewData["Title"] = "Registro";
            return View(model);
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
                    new SelectListItem { Value = "Profesor", Text = "Profesor" }
                };
            int idRol = 0;
            var user = await _graphServiceClient.Me.GetAsync();
            model.Niveles = ObtenerNiveles();
            using (var connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {

                await connection.OpenAsync();
                if (model.RolSeleccionado == "Alumno")
                {
                    idRol = 1;
                    string queryAlumno = @"INSERT INTO Alumnos 
                        (nombre_alumno, apellido_paterno, apellido_materno, telefono_alumno, numero_de_boleta, matricula, id_azure) 
                        VALUES (@Nombre, @ApellidoPaterno, @ApellidoMaterno, @Telefono, @NumeroBoleta, @Matricula, @IdAzure)";
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
                                command.Parameters.AddWithValue("@NumeroBoleta", long.Parse(model.Alumno.NumeroBoleta.ToString()));
                                command.Parameters.AddWithValue("@Matricula", model.Alumno.Matricula.ToString());
                                command.Parameters.AddWithValue("@IdAzure", user.Id.ToString());
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
                }
                else
                {
                    int nivelSeleccionado = ObtenerNivelSeleccionado(model.Profesor.NivelImparte.ToString());
                    idRol = 2;
                    string queryProfesor = @"INSERT INTO Profesores 
                        (numero_empleado, nombre_profesor, telefono_profesor, niveles_que_imparte, id_azure) 
                        VALUES (@NumeroEmpleado, @NombreCompleto, @Telefono, @NivelImparte, @IdAzure)";
                    using (var transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            using (SqlCommand command = new SqlCommand(queryProfesor, connection, transaction))
                            {
                                command.Parameters.AddWithValue("@NumeroEmpleado", long.Parse(model.Profesor.NumeroEmpleado.ToString()));
                                command.Parameters.AddWithValue("@NombreCompleto", model.Profesor.NombreCompleto.ToString());
                                command.Parameters.AddWithValue("@Telefono", long.Parse(model.Profesor.Telefono.ToString()));
                                command.Parameters.AddWithValue("@NivelImparte", nivelSeleccionado);
                                command.Parameters.AddWithValue("@IdAzure", user.Id.ToString());
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
                }
                string queryRegistro = "INSERT INTO Registrados (id_rol, id_azure) VALUES (@Id_rol, @Id_azure)";
                using (SqlCommand command = new SqlCommand(queryRegistro, connection))
                {
                    command.Parameters.AddWithValue("@Id_rol", idRol);
                    command.Parameters.AddWithValue("@Id_azure", user.Id.ToString());
                    command.ExecuteNonQuery();
                }
                return RedirectToAction("Index", "Home");
            }
        }

        private int ObtenerNivelSeleccionado(string nivel)
        {
            switch (nivel)
            {
                case "Introductorio":
                    return 1;
                case "Basico 1":
                    return 2;
                case "Basico 2":
                    return 3;
                case "Basico 3":
                    return 4;
                case "Basico 4":
                    return 5;
                case "Basico 5":
                    return 6;
                case "Intermedio 1":
                    return 7;
                case "Intermedio 2":
                    return 8;
                case "Intermedio 3":
                    return 9;
                case "Intermedio 4":
                    return 10;
                case "Intermedio 5":
                    return 11;
                case "Avanzado 1":
                    return 12;
                case "Avanzado 2":
                    return 13;
                case "Avanzado 3":
                    return 14;
                case "Avanzado 4":
                    return 15;
                case "Avanzado 5":
                    return 16;
                case "FCE":
                    return 17;
                default:
                    throw new ArgumentException("Nivel no válido");
            }
        }
        private List<SelectListItem> ObtenerNiveles()
        {
            return new List<SelectListItem>
        {
            new SelectListItem { Value = "Introductorio", Text = "Introductorio" },
                    new SelectListItem { Value = "Basico 1", Text = "Basico 1" },
                    new SelectListItem { Value = "Basico 2", Text = "Basico 2" },
                    new SelectListItem { Value = "Basico 3", Text = "Basico 3" },
                    new SelectListItem { Value = "Basico 4", Text = "Basico 4" },
                    new SelectListItem { Value = "Basico 5", Text = "Basico 5" },
                    new SelectListItem { Value = "Intermedio 1", Text = "Intermedio 1" },
                    new SelectListItem { Value = "Intermedio 2", Text = "Intermedio 2" },
                    new SelectListItem { Value = "Intermedio 3", Text = "Intermedio 3" },
                    new SelectListItem { Value = "Intermedio 4", Text = "Intermedio 4" },
                    new SelectListItem { Value = "Intermedio 5", Text = "Intermedio 5" },
                    new SelectListItem { Value = "Avanzado 1", Text = "Avanzado 1" },
                    new SelectListItem { Value = "Avanzado 2", Text = "Avanzado 2" },
                    new SelectListItem { Value = "Avanzado 3", Text = "Avanzado 3" },
                    new SelectListItem { Value = "Avanzado 4", Text = "Avanzado 4" },
                    new SelectListItem { Value = "Avanzado 5", Text = "Avanzado 5" },
                    new SelectListItem { Value = "FCE", Text = "FCE" }
        };
        }
    }
}
