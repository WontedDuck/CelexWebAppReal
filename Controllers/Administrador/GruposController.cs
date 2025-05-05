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
            TipoGrupoModel tipoGrupo = new TipoGrupoModel();
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
                            nivelcurso = tipoGrupo.Niveles(reader["id_nivel"].ToString());
                            tipocurso = tipoGrupo.Tipo(reader["id_tipo_curso"].ToString());
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
