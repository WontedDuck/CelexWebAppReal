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
using Microsoft.AspNetCore.Http;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;


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
        string tipousuario = "";
        using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
        {
            await connection.OpenAsync();
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
                using (SqlCommand command = new SqlCommand("SELECT id_registrado, id_rol FROM Registrados WHERE id_azure = @id", connection))
                {
                    command.Parameters.AddWithValue("@id", user.Id.ToString());
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            HttpContext.Session.SetString("id_registrado", reader.GetInt32(0).ToString());
                            HttpContext.Session.SetString("id_rol", reader.GetInt32(1).ToString());
                            switch (reader.GetInt32(1))
                            {
                                case 1:
                                    tipousuario = "Alumno";
                                    break;
                                case 2:
                                    tipousuario = "Profesor";
                                    break;
                                case 3:
                                    tipousuario = "Administrador";
                                    break;
                                case 4:
                                    tipousuario = "Espera";
                                    break;
                            }
                        }
                    }
                }
            }
        }
        if(tipousuario != "")
            return RedirectToAction("Index", tipousuario);
        return View();
    }

    public IActionResult Privacy()
    {

        return View();
    }
    public async Task<IActionResult> BorrarCuenta()
    {
        try
        {
            string id_registrado = HttpContext.Session.GetString("id_registrado"),
                id_rol = HttpContext.Session.GetString("id_rol");
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                switch (int.Parse(id_rol))
                {
                    case 1:
                        await BorradoEspecifico(connection, id_registrado, int.Parse(id_rol));
                        break;
                    case 2:
                        await BorradoEspecifico(connection, id_registrado, int.Parse(id_rol)); break;
                    case 3:
                        await BorradoEspecifico(connection, id_registrado, int.Parse(id_rol)); break;
                    case 4:
                        return RedirectToAction("Index");
                }
                await BorrardoGeneral(id_registrado, connection);
            }
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            return RedirectToAction("Index");
        }
    }
    static async Task BorrardoGeneral(string id, SqlConnection connection)
    {
        try
        {
            using (SqlCommand command1 = new SqlCommand("DELETE Mensajes WHERE id_remitente = @id", connection))
            {
                command1.Parameters.AddWithValue("@id", id);
                await command1.ExecuteNonQueryAsync();
            }
            using (SqlCommand command2 = new SqlCommand("DELETE Mensajes WHERE id_destinatario = @id", connection))
            {
                command2.Parameters.AddWithValue("@id", id);
                await command2.ExecuteNonQueryAsync();
            }
            using (SqlCommand command = new SqlCommand("DELETE Registrados WHERE id_registrado = @id_registrado", connection))
            {
                command.Parameters.AddWithValue("@id_registrado", id);
                await command.ExecuteNonQueryAsync();
            }
        }
        catch (Exception ex)
        {
            return;
        }
    }
    static async Task BorradoEspecifico(SqlConnection connection, string id, int rol)
    {
        try
        {
            string usuario = "", query = "";
            switch (rol)
            {
                case 1:
                    usuario = "Alumnos";
                    query = "DELETE FROM Historial_Avance_Alumnos WHERE id_estudiantes = (SELECT id_estudiantes FROM Alumnos WHERE id_registrado = @Id_registrado)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id_registrado", id);
                        await command.ExecuteNonQueryAsync();
                    }
                    query = "DELETE FROM Avance_Alumnos WHERE id_estudiantes = (SELECT id_estudiantes FROM Alumnos WHERE id_registrado = @Id_registrado)";
                    using (SqlCommand command2 = new SqlCommand(query, connection))
                    {
                        command2.Parameters.AddWithValue("@Id_registrado", id);
                        await command2.ExecuteNonQueryAsync();
                    }
                    query = "DELETE FROM Alumnos WHERE id_registrado = @Id_registrado";
                    using (SqlCommand command3 = new SqlCommand(query, connection))
                    {
                        command3.Parameters.AddWithValue("@Id_registrado", id);
                        await command3.ExecuteNonQueryAsync();
                    }
                    break;
                case 2:
                    usuario = "Profesores";
                    int regnew = 0;
                    query = "INSERT INTO Registrados (id_rol, id_azure, fecha_registro) OUTPUT INSERTED.id_registrado VALUES(@Id_rol, @Id_azure, @Fecha_registro);";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id_rol", 2);
                        command.Parameters.AddWithValue("@Id_azure", $"Exprofesor_{id}");
                        command.Parameters.AddWithValue("@Fecha_registro", DateTime.Now);
                        regnew = (int)await command.ExecuteScalarAsync();
                    }
                    query = "UPDATE Profesores SET id_registrado = @idProf WHERE id_registrado = @id";
                    using (SqlCommand command2 = new SqlCommand(query, connection))
                    {
                        command2.Parameters.AddWithValue("@idProf", regnew);
                        command2.Parameters.AddWithValue("@id", id);
                        await command2.ExecuteNonQueryAsync();
                    }
                    break;
                case 3:
                    usuario = "Administradores";
                    break;
            }
        } catch(Exception ex)
        {
            return;
        }
    }
    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
