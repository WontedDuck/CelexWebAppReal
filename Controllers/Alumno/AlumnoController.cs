using CelexWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;

namespace CelexWebApp.Controllers.Alumno
{
    public class AlumnoController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public AlumnoController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
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
            int id_registrado = int.Parse(HttpContext.Session.GetString("id_registrado"));
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Alumnos WHERE id_registrado = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id_registrado);
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        ViewData["alumno"] = reader["nombre_alumno"].ToString();
                    }
                }
            }
            return View();
        }
    }
}
