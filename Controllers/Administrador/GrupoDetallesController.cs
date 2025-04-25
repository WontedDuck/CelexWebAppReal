using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using CelexWebApp.Models.ModelGrupos;
using CelexWebApp.Models.ProfesorMMV;
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
                profesor = await extraerInformacion.ProfesorInfo(profesor);
                grupo.Alumnos = await extraerInformacion.AlumnosInfo();
            }
            grupo = await extraerInformacion.GrupoInfo(id, profesor);
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
            HttpContext.Session.SetString("nombre_profesor", nombre.ToString());
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
            string nombre_profesor = HttpContext.Session.GetString("nombre_profesor");
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
                nombre_profesor = await extraerInformacion.ProfesorInfo(nombre_profesor);
                grupo.Alumnos = await extraerInformacion.AlumnosInfo();
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            return View();
        }
        public async Task<IActionResult> BajaAlumno()
        {
            return View();
        }
    }
}
