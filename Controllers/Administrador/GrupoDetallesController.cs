using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using CelexWebApp.Models.ModelGrupos;
using CelexWebApp.Models.ProfesorMMV;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            string profesor = "";
            HttpContext.Session.SetString("id_curso", id.ToString());
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id);
            if (TempData["Informacion"] == "Informacion")
            {
                HttpContext.Session.SetString("id_profesor", extraerInformacion.ProfesorId());
                HttpContext.Session.SetString("profesor_nombre", await extraerInformacion.ProfesorNombre(profesor));
                profesor = HttpContext.Session.GetString("profesor_nombre");
            }
            grupo = await extraerInformacion.GrupoInfo(id, profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = await extraerInformacion.AlumnosInfo();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModel>();
            }
            return View(grupo);
        }
        public async Task<IActionResult> BuscarProfesor()
        {
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            return View(await extraerInformacion.BusquedaProfesor());
        }
        public async Task<IActionResult> AgregarProfesor(int id, string nombre)
        {
            HttpContext.Session.SetString("id_profesor", id.ToString());
            HttpContext.Session.SetString("profesor_nombre", nombre.ToString());
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre);
            return View("Index", grupo);
        }
        public async Task<IActionResult> BuscarAlumnos()
        {
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            return View(await extraerInformacion.BusquedaAlumno());
        }
        public async Task<IActionResult> AgregarAlumno(int id)
        {
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            int id_profesor = int.Parse(HttpContext.Session.GetString("id_profesor"));
            string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Avance_Alumnos (id_estudiantes, id_cursos, id_profesor) VALUES (@id, @id_curso, @id_profesor)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.Parameters.AddWithValue("@id_curso", id_curso);
                    command.Parameters.AddWithValue("@id_profesor", id_profesor);
                    await command.ExecuteNonQueryAsync();
                }
                string query2 = "UPDATE Curso SET ocupados = ISNULL(ocupados, 0) + 1 WHERE id_cursos = @id_curso;";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id_curso", id_curso);
                    await command.ExecuteNonQueryAsync();
                }
            }
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            if (TempData["Informacion"] == "Informacion")
            {
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = await extraerInformacion.AlumnosInfo();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModel>();
            }
            return View("Index", grupo);
        }
        public async Task<IActionResult> BajaAlumno(int id)
        {
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            int id_profesor = int.Parse(HttpContext.Session.GetString("id_profesor"));
            string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            await extraerInformacion.BorrarAlumno(id, id_curso);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            if (TempData["Informacion"] == "Informacion")
            {
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = await extraerInformacion.AlumnosInfo();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModel>();
            }
            return View("Index", grupo);
        }
        public async Task<IActionResult> Calificaciones(int id)
        {
            AlumnoModelView calificaciones = new AlumnoModelView();
            AlumnoModel alumno = new AlumnoModel();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Alumnos WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        if (reader.Read())
                        {
                            alumno.Id = int.Parse(reader["id_estudiantes"].ToString());
                            alumno.Nombre = reader["nombre_alumno"].ToString();
                            alumno.ApellidoPa = reader["apellido_paterno"].ToString();
                            alumno.ApellidoMa = reader["apellido_materno"].ToString();
                        }
                }
                string query2 = "SELECT calcontinua, calexmedia, caliexfinal FROM Avance_Alumnos WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        if (reader.Read())
                        {
                            calificaciones.CalContinua = reader["calcontinua"] != DBNull.Value ? Convert.ToInt32(reader["calcontinua"]) : 0;
                            calificaciones.CalExMedia = reader["calexmedia"] != DBNull.Value ? Convert.ToInt32(reader["calexmedia"]) : 0;
                            calificaciones.CaliExFinal = reader["caliexfinal"] != DBNull.Value ? Convert.ToInt32(reader["caliexfinal"]) : 0;
                        }
                }
            }
            calificaciones.alumno = alumno;
            return View(calificaciones);
        }
        [HttpPost]
        public async Task<IActionResult> ActualizarInformacion(AlumnoModelView alumnoModelView)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "UPDATE Avance_Alumnos SET calcontinua = @calcontinua, calexmedia = @calexmedia, caliexfinal = @caliexfinal WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@calcontinua", alumnoModelView.CalContinua);
                    command.Parameters.AddWithValue("@calexmedia", alumnoModelView.CalExMedia);
                    command.Parameters.AddWithValue("@caliexfinal", alumnoModelView.CaliExFinal);
                    command.Parameters.AddWithValue("@id", alumnoModelView.alumno.Id);
                    command.ExecuteNonQuery();
                }
            }
            return View("Calificaciones", alumnoModelView);
        }

    }
}
