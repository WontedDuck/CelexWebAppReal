using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using System.Threading.Tasks;


namespace CelexWebApp.Controllers.Administrador
{

    [Authorize]
    public class GruposController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public GruposController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
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
            string nivelcurso = "", tipocurso = "";
            var grupos = new List<GrupoModel>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Curso";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {                        
                        while (await reader.ReadAsync())
                        {
                            switch (reader["id_nivel"].ToString())
                            {
                                case "1":
                                    nivelcurso = "Introductorio"; break;
                                case "2":
                                    nivelcurso = "Basico1"; break;
                                case "3":
                                    nivelcurso = "Basico2"; break;
                                case "4":
                                    nivelcurso = "Basico3"; break;
                                case "5":
                                    nivelcurso = "Basico4"; break;
                                case "6":
                                    nivelcurso = "Basico5"; break;
                                case "7":
                                    nivelcurso = "Intermedio1"; break;
                                case "8":
                                    nivelcurso = "Intermedio2"; break;
                                case "9":
                                    nivelcurso = "Intermedio3"; break;
                                case "10":
                                    nivelcurso = "Intermedio4"; break;
                                case "11":
                                    nivelcurso = "Intermedio5"; break;
                                case "12":
                                    nivelcurso = "Avanzado1"; break;
                                case "13":
                                    nivelcurso = "Avanzado2"; break;
                                case "14":
                                    nivelcurso = "Avanzado3"; break;
                                case "15":
                                    nivelcurso = "Avanzado4"; break;
                                case "16":
                                    nivelcurso = "Avanzado5"; break;
                                case "17":
                                    nivelcurso = "FCE"; break;
                            }
                            switch (reader["id_tipo_curso"].ToString())
                            {
                                case "1":
                                    tipocurso = "Semanal"; break;
                                case "2":
                                    tipocurso = "Sabatino"; break;
                                case "3":
                                    tipocurso = "Intensivo"; break;
                            }
                            grupos.Add(new GrupoModel
                            {
                                Id = Convert.ToInt32(reader["id_cursos"]),
                                Nombre = reader["nombre_curso"].ToString(),
                                Nivel = nivelcurso,
                                TipoCurso = tipocurso,
                                FechaInicio = Convert.ToDateTime(reader["fecha_inicio"]),
                                FechaFin = Convert.ToDateTime(reader["fecha_fin"]),
                                capacidad = Convert.ToInt32(reader["capacidad"])
                            });
                        }
                    }
                }
            }
            return View(grupos);
        }
    }
}
