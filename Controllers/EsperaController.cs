using CelexWebApp.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CelexWebApp.Controllers
{
    public class EsperaController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public EsperaController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
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
            var user = _graphServiceClient.Me.GetAsync().Result;
            ViewData["id_azure"] = user.Id.ToString();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> RegresarMenu()
        {
            int id_usuario = 0;
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                using (SqlCommand command1 = new SqlCommand("SELECT id_registrado FROM Registrados WHERE id_azure = @id", connection))
                {
                    command1.Parameters.AddWithValue("@id", _graphServiceClient.Me.GetAsync().Result.Id.ToString());
                    using (SqlDataReader reader = command1.ExecuteReader())
                        if (reader.Read())
                            id_usuario = reader.GetInt32(0);
                }
                using (SqlCommand command2 = new SqlCommand("DELETE Mensajes WHERE id_remitente = @id", connection))
                {
                    command2.Parameters.AddWithValue("@id", id_usuario);
                    await command2.ExecuteNonQueryAsync();
                }
                using (SqlCommand command3 = new SqlCommand("DELETE Mensajes WHERE id_destinatario = @id", connection))
                {
                    command3.Parameters.AddWithValue("@id", id_usuario);
                    await command3.ExecuteNonQueryAsync();
                }
                using (SqlCommand command = new SqlCommand("DELETE FROM Registrados WHERE id_azure = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", _graphServiceClient.Me.GetAsync().Result.Id.ToString());
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
