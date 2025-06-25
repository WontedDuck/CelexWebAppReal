using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using CelexWebApp.Models.NotificacionesMMV;
using CelexWebApp.Models.ProfesorMMV;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Azure;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using QuestPDF.Fluent;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
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
            ViewData["Notificaciones"] = notificaciones;

            var user = await _graphServiceClient.Me.GetAsync();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                connection.Open();
                string query = "SELECT * FROM Administrador WHERE id_registrado = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        HttpContext.Session.SetString("id", reader["id_administrador"].ToString());
                        HttpContext.Session.SetString("nombre", reader["nombre"].ToString());
                        HttpContext.Session.SetString("telefono", reader["telefono"].ToString());
                    }
                }
            }
            return View();
        }

        [HttpPost]

        public async Task<IActionResult> EnviarNotificacion(string mensaje, string destinatarios)
        {
            int rol = 0;
            string query = "SELECT id_registrado FROM Registrados WHERE id_rol = @rol";
            switch (destinatarios)
            {
                case "Todos":
                    query = "SELECT id_registrado FROM Registrados WHERE id_rol != @rol";
                    rol = 3;
                    break;
                case "Alumnos":
                    rol = 1;
                    break;
                case "Profesores":
                    rol = 2;
                    break;
            }
            try
            {
                using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    await connection.OpenAsync();
                    List<int> ndestinatarios = new List<int>();
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@rol", rol);

                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ndestinatarios.Add(Convert.ToInt32(reader["id_registrado"]));
                            }
                        }
                    }
                    foreach (int idDestinatario in ndestinatarios)
                    {
                        string query2 = "INSERT INTO Mensajes (id_remitente, id_destinatario, contenido, fecha_registro) VALUES (@Id_remitente, @Id_destinatario, @Contenido, @Fecha_registro)";
                        using (SqlCommand command2 = new SqlCommand(query2, connection))
                        {
                            command2.Parameters.AddWithValue("@Id_remitente", int.Parse(HttpContext.Session.GetString("id_registrado")));
                            command2.Parameters.AddWithValue("@Id_destinatario", idDestinatario);
                            command2.Parameters.AddWithValue("@Contenido", $"Mensaje de Administrador: {mensaje}");
                            command2.Parameters.AddWithValue("@Fecha_registro", DateTime.Now);
                            await command2.ExecuteNonQueryAsync();
                        }
                    }
                }
                TempData["MensajeEstado"] = "El mensaje fue enviado correctamente.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["MensajeEstado"] = $"Error al enviar el mensaje, enviar de nuevo: {ex.Message}";

                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AgregarProfesor(int numeroEmpleado, string nombreProfesor, string telefonoProfesor, string idAzure)
        {
            int idAzureInt = 0;
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM Profesores WHERE numero_empleado = @NumeroEmpleado";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumeroEmpleado", numeroEmpleado);
                    int count = (int)await command.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        TempData["MensajeEstadoAgregar"] = "El profesor ya existe en la base de datos.";
                        return RedirectToAction("Index");
                    }
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT id_registrado FROM Registrados WHERE id_azure = @IdAzure";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdAzure", idAzure);
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    if (reader.Read())
                    {
                        idAzureInt = Convert.ToInt32(reader["id_registrado"]);
                    }
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Profesores (numero_empleado, nombre_profesor, telefono_profesor, id_registrado) VALUES (@NumeroEmpleado, @NombreProfesor, @TelefonoProfesor, @IdAzure)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@NumeroEmpleado", numeroEmpleado);
                    command.Parameters.AddWithValue("@NombreProfesor", nombreProfesor);
                    command.Parameters.AddWithValue("@TelefonoProfesor", telefonoProfesor);
                    command.Parameters.AddWithValue("@IdAzure", idAzureInt);
                    command.ExecuteNonQuery();
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "UPDATE Registrados SET id_rol = 2 WHERE id_registrado = @IdAzure";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@IdAzure", idAzureInt);
                    command.ExecuteNonQuery();
                }
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Mensajes (id_remitente, id_destinatario, contenido, fecha_registro) VALUES (@Id_remitente, @Id_destinatario, @Contenido, @Fecha_registro)";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_remitente", int.Parse(HttpContext.Session.GetString("id_registrado")));
                    command.Parameters.AddWithValue("@Id_destinatario", idAzureInt);
                    command.Parameters.AddWithValue("@Contenido", $"Hola {nombreProfesor}, has sido agregado como profesor.");
                    command.Parameters.AddWithValue("@Fecha_registro", DateTime.Now);
                    await command.ExecuteNonQueryAsync();
                }
            }
            TempData["MensajeEstadoAgregar"] = $"El profesor {nombreProfesor} se a agregado exitosamente";
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> CrearGrupo(string nombreGrupo, string nivelGrupo, string tipoCurso, DateTime fechaInicio, DateTime fechaFin, int cantidadEstudiantes)
        {
            try
            {
                int idnivel, idtipo;
                switch(nivelGrupo)
                {
                    case "Introductorio":
                        idnivel = 1; break;
                    case "Basico1":
                        idnivel = 2; break;
                    case "Basico2":
                        idnivel = 3; break;
                    case "Basico3":
                        idnivel = 4; break;
                    case "Basico4":
                        idnivel = 5; break;
                    case "Basico5":
                        idnivel = 6; break;
                    case "Intermedio1":
                        idnivel = 7; break;
                    case "Intermedio2":
                        idnivel = 8; break;
                    case "Intermedio3":
                        idnivel = 9; break;
                    case "Intermedio4":
                        idnivel = 10; break;
                    case "Intermedio5":
                        idnivel = 11; break;
                    case "Avanzado1":
                        idnivel = 12; break;
                    case "Avanzado2":
                        idnivel = 13; break;
                    case "Avanzado3":
                        idnivel = 14; break;
                    case "Avanzado4":
                        idnivel = 15; break;
                    case "Avanzado5":
                        idnivel = 16; break;
                    case "FCE":
                        idnivel = 17; break;
                    default:
                        TempData["MensajeEstadoCrearGrupo"] = "Nivel de Curso no seleccionado";
                        return RedirectToAction("Index");
                }
                switch (tipoCurso)
                {
                    case "Semanal":
                        idtipo = 1; break;
                    case "Sabatino":
                        idtipo = 2; break;
                    case "Intensivo":
                        idtipo = 3; break;
                    default:
                        TempData["MensajeEstadoCrearGrupo"] = "Tipo de Curso no seleccionado";
                        return RedirectToAction("Index");
                }
                if (fechaInicio <= DateTime.Now)
                {
                    TempData["MensajeEstadoCrearGrupo"] = "La fecha de inicio debe ser mayor a hoy";
                    return RedirectToAction("Index");
                }
                if (fechaInicio <= fechaFin.AddMonths(-2))
                {
                    TempData["MensajeEstadoCrearGrupo"] = "La fecha debe ser al menos dos meses antes de la final";
                    return RedirectToAction("Index");
                }
                if (fechaInicio >= fechaFin)
                {
                    TempData["MensajeEstadoCrearGrupo"] = "La fecha de inicio debe ser anterior a la fecha de fin.";
                    return RedirectToAction("Index");
                }
                if (cantidadEstudiantes >= 50)
                {
                    TempData["MensajeEstadoCrearGrupo"] = "El grupo tiene demaciados estudiantes";
                    return RedirectToAction("Index");
                }
                using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    await connection.OpenAsync();
                    string query = @"
                INSERT INTO Curso (nombre_curso, id_nivel, id_tipo_curso, fecha_inicio, fecha_fin, capacidad) 
                VALUES (@NombreCurso, @Id_Nivel, @Id_TipoCurso, @FechaInicio, @FechaFin, @Capacidad)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NombreCurso", nombreGrupo);
                        command.Parameters.AddWithValue("@Id_Nivel", idnivel);
                        command.Parameters.AddWithValue("@Id_TipoCurso", idtipo);
                        command.Parameters.AddWithValue("@FechaInicio", fechaInicio);
                        command.Parameters.AddWithValue("@FechaFin", fechaFin);
                        command.Parameters.AddWithValue("@Capacidad", cantidadEstudiantes);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["MensajeEstadoCrearGrupo"] = "El grupo se creó exitosamente.";
                return RedirectToAction("Index");
            }   
            catch (Exception ex)
            {
                TempData["MensajeEstadoCrearGrupo"] = $"Error al crear el grupo: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
        public IActionResult MarcarComoLeida(string id_mensaje)
        {
            using (SqlConnection connection = new SqlConnection(_conexion.GetConexionAsync().Result))
            {
                connection.Open();
                string query = "UPDATE Mensajes SET leido = 1 WHERE id_mensaje = @Id_mensaje";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_mensaje", id_mensaje);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
        public IActionResult BorrarNotificacion(string id_mensaje)
        {
            using (SqlConnection connection = new SqlConnection(_conexion.GetConexionAsync().Result))
            {
                connection.Open();
                string query = "DELETE FROM Mensajes WHERE id_mensaje = @Id_mensaje";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_mensaje", id_mensaje);
                    command.ExecuteNonQuery();
                }
            }
            return RedirectToAction("Index");
        }
        [HttpGet]
        public async Task<IActionResult> GenerarHistorialVista(string busquedaId)
        {
            var alumnos = new List<AlumnoModel>();

            if (!string.IsNullOrWhiteSpace(busquedaId))
            {
                using (var connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    connection.Open();
                    var query = @"SELECT DISTINCT a.id_estudiantes, a.nombre_alumno, a.apellido_paterno, a.apellido_materno, a.matricula 
                                 FROM Alumnos a 
                                 INNER JOIN Historial_Avance_Alumnos h ON a.id_estudiantes = h.id_estudiantes 
                                 WHERE a.id_estudiantes = @IdAlumno";

                    using (var command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@IdAlumno", busquedaId);

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                alumnos.Add(new AlumnoModel
                                {
                                    Id = reader.GetInt16(0),
                                    Nombre = $"{reader.GetString(1)} {reader.GetString(2)} {reader.GetString(3)}",
                                    Matricula = reader.GetString(4)
                                });
                            }
                        }
                    }
                }
            }

            ViewData["Alumnos"] = alumnos;
            return View("Index");
        }
        [HttpGet]
        public async Task<IActionResult> GenerarHistorial(int idAlumno)
        {
            decimal calificaciontotaltotal = 0, asistenciaTotal = 0;
            var historiales = new List<HistorialAvanceAlumno>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Historial_Avance_Alumnos WHERE id_estudiantes = @Id_Estudiante ORDER BY id_nivel ASC";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_Estudiante", idAlumno);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var avance = new HistorialAvanceAlumno
                            {
                                IdEstudiante = Convert.ToInt32(reader["id_estudiantes"]),
                                IdProfesor = Convert.ToInt32(reader["id_profesor"]),
                                IdCurso = Convert.ToInt32(reader["id_cursos"]),
                                Asistencia = Convert.ToInt32(reader["asistencia"]),
                                CalContinua = Convert.ToDecimal(reader["calcontinua"]),
                                CalExMedia = Convert.ToDecimal(reader["calexmedia"]),
                                CalExFinal = Convert.ToDecimal(reader["caliexfinal"])
                            };
                            avance.CalificacionTotal = ((avance.CalContinua + avance.CalExMedia + avance.CalExFinal) / 30) * 100;
                            calificaciontotaltotal += avance.CalificacionTotal;
                            asistenciaTotal += avance.Asistencia;
                            historiales.Add(avance);
                        }
                    }
                }
                TipoGrupoModel tipoGrupoModel = new TipoGrupoModel();
                calificaciontotaltotal = calificaciontotaltotal / historiales.Count;
                string query2 = "SELECT * FROM Curso WHERE id_cursos = @Id_curso";
                foreach (var historial in historiales)
                {
                    using (SqlCommand command = new SqlCommand(query2, connection))
                    {
                        command.Parameters.AddWithValue("@Id_curso", historial.IdCurso);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                historial.Modulo = tipoGrupoModel.Niveles(reader["id_nivel"].ToString());
                                if (int.Parse(reader["id_nivel"].ToString()) <= 2)
                                {
                                    historial.NivelMCER = "A1";
                                }
                                else if (int.Parse(reader["id_nivel"].ToString()) <= 5)
                                {
                                    historial.NivelMCER = "A2";
                                }
                                else if (int.Parse(reader["id_nivel"].ToString()) <= 10)
                                {
                                    historial.NivelMCER = "B1";
                                }
                                else if (int.Parse(reader["id_nivel"].ToString()) <= 15)
                                {
                                    historial.NivelMCER = "B2";
                                }
                                else
                                {
                                    historial.NivelMCER = "C1";
                                }
                                historial.NombreCurso = reader["nombre_curso"].ToString();
                                historial.Periodo = $"{DateTime.Parse(reader["fecha_inicio"].ToString()):dd MMMM yyyy} - {DateTime.Parse(reader["fecha_fin"].ToString()):dd MMMM yyyy}";
                            }
                        }
                    }
                }
                string query3 = "SELECT * FROM Profesores WHERE id_profesor = @Id_profesor";
                foreach (var historial in historiales)
                {
                    using (SqlCommand command = new SqlCommand(query3, connection))
                    {
                        command.Parameters.AddWithValue("@Id_profesor", historial.IdProfesor);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                historial.NombreProfesor = reader["nombre_profesor"].ToString();
                            }
                        }
                    }
                }
                string query4 = "SELECT * FROM Alumnos WHERE id_estudiantes = @Id_estudiante";
                foreach (var historial in historiales)
                {
                    using (SqlCommand command = new SqlCommand(query4, connection))
                    {
                        command.Parameters.AddWithValue("@Id_estudiante", historial.IdEstudiante);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                historial.Nombre = reader["nombre_alumno"].ToString() + " " + reader["apellido_paterno"].ToString() + " " + reader["apellido_materno"].ToString();
                                historial.Boleta = reader["matricula"].ToString();
                            }
                        }
                    }
                }
            }
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var documento = new HistorialCelexDocument
            {
                calificaciontotaltotal = calificaciontotaltotal,
                asistenciaTotal = asistenciaTotal,
                Fecha = DateTime.Now,
                Historial = historiales
            };

            var pdfBytes = documento.GeneratePdf();
            return File(pdfBytes, "application/pdf", "Historial_Celex.pdf");
        }
        public async Task<IActionResult> GenerarHistorialProfesor()
        {
            List<ProfesorModel> profesores = new List<ProfesorModel>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Profesores";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ProfesorModel profesor = new ProfesorModel
                            {
                                Id = reader.GetInt16(0),
                                Numero_Empleado = Convert.ToInt32(reader.GetDecimal(2)),
                                Nombre = reader.GetString(3),
                                Telefono = reader.GetString(4),
                            };
                            profesores.Add(profesor);
                        }
                    }
                }
            }
            return View("HistorialProfesor", profesores);
        }
        public async Task<IActionResult> HistorialProfesor(int id, string nombre)
        {
            List<int> ids = new List<int>();
            TipoGrupoModel tipoGrupoModel = new TipoGrupoModel();
            List<GrupoDetallesModel> grupos = new List<GrupoDetallesModel>();
            using(SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT DISTINCT id_cursos FROM Historial_Avance_Alumnos WHERE id_profesor = @Id_profesor";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_profesor", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            ids.Add(reader.GetInt16(0));
                        }
                    }
                }
                foreach (var curso in ids)
                {
                    string query2 = "SELECT * FROM Historial_Curso WHERE id_curso_origen = @Id_curso AND id_profesor = @Id_profesor";
                    using (SqlCommand command = new SqlCommand(query2, connection))
                    {
                        command.Parameters.AddWithValue("@Id_curso", curso);
                        command.Parameters.AddWithValue("@Id_profesor", id);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                var cursoModel = new GrupoDetallesModel
                                {
                                    Id = reader.GetInt16(1),
                                    Nivel = tipoGrupoModel.Niveles(reader.GetByte(2).ToString()),
                                    TipoCurso = tipoGrupoModel.Tipo(reader.GetByte(3).ToString()),
                                    FechaInicio = reader.GetDateTime(4),
                                    FechaFin = reader.GetDateTime(5),
                                    Ocupados = Convert.ToInt32(reader.GetDecimal(7)),
                                    Nombre = reader.GetString(8)
                                };
                                grupos.Add(cursoModel);
                            }
                        }
                    }
                }
            }
            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var documento = new HistorialProfesorDocument(grupos);
            var pdfBytes = documento.GeneratePdf();
            return File(pdfBytes, "application/pdf", $"Historial_Profesor_{nombre}.pdf");

        }
    }
}