using CelexWebApp.Models;
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
            var user = await _graphServiceClient.Me.GetAsync();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                connection.Open();
                string query = "SELECT * FROM Profesores WHERE id_registrado = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        //Guardar la informacion del administrador de manera local para su uso durante todo el proyecto
                        HttpContext.Session.SetString("id", reader["id_profesor"].ToString());
                        HttpContext.Session.SetString("nombre", reader["nombre_profesor"].ToString());
                        HttpContext.Session.SetString("telefono", reader["telefono_profesor"].ToString());
                    }
                }
            }
            ViewData["Id"] = HttpContext.Session.GetString("id");
            ViewData["Nombre"] = HttpContext.Session.GetString("nombre");
            ViewData["Telefono"] = HttpContext.Session.GetString("telefono");
            ViewData["Nivel"] = HttpContext.Session.GetString("nivel");
            return View();
        }
    }
}
