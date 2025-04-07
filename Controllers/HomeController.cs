using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CelexWebApp.Models;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Identity.Abstractions;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Threading.Tasks;
using CelexWebApp.Models.RegistroMV;


namespace CelexWebApp.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IDownstreamApi _downstreamApi;
    private readonly GraphServiceClient _graphServiceClient;
    private readonly ILogger<HomeController> _logger;
    private readonly Conexion _conexion;

    public HomeController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
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
        int result = 0;

        using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
        {
            connection.Open();
            using (SqlCommand command = new SqlCommand("SELECT COUNT(*) FROM Registrados WHERE id_azure = @id", connection))
            {
                command.Parameters.AddWithValue("@id", user.Id.ToString());
                result = (int)await command.ExecuteScalarAsync();
            }
            if (result == 0)
            {
                return RedirectToAction("Index", "Registro");
            }
            else
            {
                using (SqlCommand command = new SqlCommand("SELECT id_rol FROM Registrados WHERE id_azure = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", user.Id.ToString());
                    int rol = (int)await command.ExecuteScalarAsync();
                    if (rol == 1)
                    {
                        return RedirectToAction("Index", "Alumno");
                    }
                    else if (rol == 2)
                    {
                        return RedirectToAction("Index", "Profesor");
                    }
                    else if (rol == 3)
                    { 
                        return RedirectToAction("Index", "Admin");
                    }
                }
            }
        }
        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
