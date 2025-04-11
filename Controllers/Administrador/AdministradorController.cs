using CelexWebApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
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
                string query = "SELECT * FROM Administrador WHERE id_azure = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", user.Id.ToString());
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
            ViewData["Id"] = HttpContext.Session.GetString("id");
            ViewData["Nombre"] = HttpContext.Session.GetString("nombre");
            ViewData["Telefono"] = HttpContext.Session.GetString("telefono");
            return View();
        }
    }
}
