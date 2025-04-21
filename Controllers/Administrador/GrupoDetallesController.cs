using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.ProfesorMMV;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;

namespace CelexWebApp.Controllers.Administrador
{
    public class GrupoDetallesController : Controller
    {
        GrupoDetallesModel grupo = new GrupoDetallesModel();
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public GrupoDetallesController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Index(int id)
        {
            HttpContext.Session.SetString("id_curso", id.ToString());
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Curso WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            grupo = new GrupoDetallesModel()
                            {
                                Nombre = reader["nombre_curso"].ToString(),
                                Nivel = reader["id_nivel"].ToString(),
                                TipoCurso = reader["id_tipo_curso"].ToString(),
                                FechaInicio = DateTime.Parse(reader["fecha_inicio"].ToString()),
                                FechaFin = DateTime.Parse(reader["fecha_fin"].ToString()),
                                Capacidad = int.Parse(reader["capacidad"].ToString()),
                                Ocupados = reader["ocupados"] != DBNull.Value ? int.Parse(reader["ocupados"].ToString()) : 0
                            };
                        }
                    }
                }
                string query2 = "SELECT * FROM Avance_Alumnos WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            TempData["Informacion"] = "Grupo Vacio";
                        }
                    }
                }
            }
            return View(grupo);
        }
        public async Task<IActionResult> BuscarProfesor()
        {
            var profesores = new List<ProfesorModel>();

            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();

                string query = "SELECT * FROM Profesores WHERE niveles_que_imparte IS NULL";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            profesores.Add(new ProfesorModel
                            {
                                Id = int.Parse(reader["id_profesor"].ToString()),
                                Numero_Empleado = int.Parse(reader["numero_empleado"].ToString()),
                                Nombre = reader["nombre_profesor"].ToString(),
                                Telefono = reader["telefono_profesor"].ToString(),
                            });
                        }
                    }
                }
            }
            return View(profesores);
        }
        public async Task<IActionResult> AgregarProfesor(int id, string nombre)
        {
            HttpContext.Session.SetString("id_profesor", id.ToString());
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Curso WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id_curso);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            grupo = new GrupoDetallesModel()
                            {
                                Profesor = nombre,
                                Nombre = reader["nombre_curso"].ToString(),
                                Nivel = reader["id_nivel"].ToString(),
                                TipoCurso = reader["id_tipo_curso"].ToString(),
                                FechaInicio = DateTime.Parse(reader["fecha_inicio"].ToString()),
                                FechaFin = DateTime.Parse(reader["fecha_fin"].ToString()),
                                Capacidad = int.Parse(reader["capacidad"].ToString()),
                                Ocupados = reader["ocupados"] != DBNull.Value ? int.Parse(reader["ocupados"].ToString()) : 0
                            };
                        }
                    }
                }
                string query2 = "SELECT * FROM Avance_Alumnos WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (!await reader.ReadAsync())
                        {
                            TempData["Informacion"] = "Grupo Vacio";
                        }
                    }
                }
            }
            return View("Index", grupo);
        }
        public async Task<IActionResult> AgregarAlumno()
        {
            return View();
        }
        public async Task<IActionResult> BajaAlumno()
        {
            return View();
        }
    }
}
