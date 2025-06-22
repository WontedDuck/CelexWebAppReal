using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using CelexWebApp.Models.NotificacionesMMV;
using CelexWebApp.Models.ProfesorMMV;
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
            List<NotificacionesModel> notificaciones = new List<NotificacionesModel>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query2 = "SELECT id_mensaje, contenido, fecha_registro, leido FROM Mensajes WHERE id_destinatario = @Id_destinatario ORDER BY leido ASC, fecha_registro DESC";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@Id_destinatario", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            NotificacionesModel notificacion = new NotificacionesModel
                            {
                                Id_Mensaje = reader.GetInt32(0),
                                Contenido = reader.GetString(1),
                                Fecha_Registro = reader.GetDateTime(2),
                                Leido = reader.GetBoolean(3)
                            };
                            notificaciones.Add(notificacion);
                        }
                    }
                }
            }
            GrupoDetallesModel grupoDetalles = null;
            ViewData["Notificaciones"] = notificaciones;
            var user = await _graphServiceClient.Me.GetAsync();
            grupoDetalles = new GrupoDetallesModel
            {
                Id = 0,
                Nombre = "Grupo no asignado",
                Nivel = "Nivel no asignado",
                TipoCurso = "Tipo de Curso no asignado",
                FechaInicio = DateTime.Now,
                FechaFin = DateTime.Now,
                Profesor = "0",
                Alumnos = new List<AlumnoModelView>(),
            };
            grupoDetalles.Alumnos.Add(new AlumnoModelView
            {
                alumno = new AlumnoModel
                {
                    Id = 0,
                    Nombre = "Sin Nombre",
                    ApellidoPa = "Apellido Paterno no asignado",
                    ApellidoMa = "Apellido Materno no asignado",
                },
                CalContinua = 0,
                CalExMedia = 0,
                CaliExFinal = 0,
            });
            TipoGrupoModel tipoGrupo = new TipoGrupoModel();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string queryAlumno = "SELECT * FROM Alumnos WHERE id_registrado = @id";
                using (SqlCommand command = new SqlCommand(queryAlumno, connection))
                {
                    command.Parameters.AddWithValue("@id", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    {
                        if (reader.Read())
                        {
                            grupoDetalles.Alumnos[0].alumno = new AlumnoModel
                            {                                
                                Id = int.Parse(reader["id_estudiantes"].ToString()),
                                Nombre = reader["nombre_alumno"].ToString(),
                                ApellidoPa = reader["apellido_paterno"].ToString(),
                                ApellidoMa = reader["apellido_materno"].ToString(),
                            };
                        }
                    }
                    reader.Close();
                }
                HttpContext.Session.SetString("id", grupoDetalles.Alumnos[0].alumno.Id.ToString());
                string queryGrupos = "SELECT * FROM Avance_Alumnos WHERE id_estudiantes = @Id_estudiantes;";
                using (SqlCommand command = new SqlCommand(queryGrupos, connection))
                {                    
                    command.Parameters.AddWithValue("@Id_estudiantes", int.Parse(HttpContext.Session.GetString("id")));
                    SqlDataReader reader2 = await command.ExecuteReaderAsync();
                    if(reader2.Read())
                    {
                        grupoDetalles.Id = int.Parse(reader2["id_cursos"].ToString());
                        grupoDetalles.Profesor = reader2["id_profesor"].ToString();
                        grupoDetalles.Alumnos[0].CalContinua = reader2["calcontinua"] != DBNull.Value ? Convert.ToSingle(reader2["calcontinua"]) : 0f;
                        grupoDetalles.Alumnos[0].CalExMedia = reader2["calexmedia"] != DBNull.Value ? Convert.ToSingle(reader2["calexmedia"]) : 0f;
                        grupoDetalles.Alumnos[0].CaliExFinal = reader2["caliexfinal"] != DBNull.Value ? Convert.ToSingle(reader2["caliexfinal"]) : 0f;
                        grupoDetalles.Alumnos[0].alumno.Asistencia = reader2["asistencia"] != DBNull.Value ? Convert.ToDecimal(reader2["asistencia"]) : 0;
                    }
                    reader2.Close();
                }
                string queryProfesor = "SELECT nombre_profesor FROM Profesores WHERE id_profesor = @Id_Profesor;";
                using(SqlCommand command = new SqlCommand(queryProfesor, connection))
                {
                    command.Parameters.AddWithValue("@Id_Profesor", int.Parse(grupoDetalles.Profesor));
                    SqlDataReader reader3 = await command.ExecuteReaderAsync();
                    if (reader3.Read())
                    {
                        grupoDetalles.Profesor = reader3["nombre_profesor"].ToString();
                    }
                    reader3.Close();
                }
                string queryCurso = "SELECT * FROM Curso WHERE id_cursos = @Id_Cursos;";
                using (SqlCommand command = new SqlCommand(queryCurso, connection))
                {
                    command.Parameters.AddWithValue("@Id_Cursos", grupoDetalles.Id);
                    SqlDataReader reader4 = await command.ExecuteReaderAsync();
                    if (reader4.Read())
                    {
                        grupoDetalles.Nombre = reader4["nombre_curso"].ToString();
                        grupoDetalles.Nivel = tipoGrupo.Niveles(reader4["id_nivel"].ToString());
                        grupoDetalles.TipoCurso = tipoGrupo.Tipo(reader4["id_tipo_curso"].ToString());
                        grupoDetalles.FechaInicio = DateTime.Parse(reader4["fecha_inicio"].ToString());
                        grupoDetalles.FechaFin = DateTime.Parse(reader4["fecha_fin"].ToString());
                        grupoDetalles.Capacidad = int.Parse(reader4["capacidad"].ToString());
                        grupoDetalles.Ocupados = int.Parse(reader4["ocupados"].ToString());
                        grupoDetalles.FechaCreacion = DateTime.Parse(reader4["fecha_creacion"].ToString());
                    }
                    reader4.Close();
                }
            }
            if(grupoDetalles.Profesor == "0")
                grupoDetalles.Profesor = "Sin Profesor";
            return View(grupoDetalles);
        }
        public async Task<IActionResult> EnviarMensajeAdministrador(string contenido)
        {
            EnviarMensaje($"{HttpContext.Session.GetString("id")} {contenido}", 3, int.Parse(HttpContext.Session.GetString("id_registrado")));
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> EnviarMensajeProfesor(string contenido)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT nombre_alumno FROM Alumnos WHERE id_registrado = @Id_registrado";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_registrado", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            contenido = $"El alumno {reader["nombre_alumno"]}: {contenido}";
                        }
                    }
                }
            }
            EnviarMensaje(contenido , 2, int.Parse(HttpContext.Session.GetString("id_registrado")));
            return RedirectToAction("Index");
        }
        public async void EnviarMensaje(string contenido, int id, int id_registrado)
        {
            int rol = 0;
            int n = 0;
            List<int> ndestinatarios = new List<int>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query1 = $"SELECT id_registrado FROM Registrados WHERE id_rol = {id}";
                using (SqlCommand command1 = new SqlCommand(query1, connection))
                {
                    using (SqlDataReader reader = command1.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ndestinatarios.Add(Convert.ToInt32(reader["id_registrado"]));
                        }
                    }
                }
                foreach (var destinatario in ndestinatarios)
                {
                    string query2 = "INSERT INTO Mensajes (id_remitente, id_destinatario, contenido) VALUES (@Id_remitente, @Id_destinatario, @Contenido)";
                    using (SqlCommand command2 = new SqlCommand(query2, connection))
                    {
                        command2.Parameters.AddWithValue("@Id_remitente", id_registrado);
                        command2.Parameters.AddWithValue("@Id_destinatario", destinatario.ToString());
                        command2.Parameters.AddWithValue("@Contenido", contenido.ToString());
                        command2.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
