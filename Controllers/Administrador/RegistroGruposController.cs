using Microsoft.AspNetCore.Mvc;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using CelexWebApp.Models;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using System.Threading.Tasks;
using CelexWebApp.Models.NotificacionesMMV;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using QuestPDF.Fluent;
using CelexWebApp.Models.ModelGrupos;

namespace CelexWebApp.Controllers
{
    public class RegistroGruposController : Controller
    {
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public RegistroGruposController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerFiltros()
        {
            var cursos = await ObtenerCursos();
            var maestros = await GetMaestros();
            var niveles = GetNiveles();
            var tipos = GetTiposCurso();

            var filtros = new
            {
                Grupos = cursos.Select(g => new { value = g.Id.ToString(), text = g.Nombre }).ToList(),
                Maestros = maestros.Select(m => new { value = m, text = m }).ToList(),
                Niveles = niveles.Select(n => new { value = n, text = n }).ToList(),
                TiposCurso = tipos.Select(t => new { value = t, text = t }).ToList()
            };
            return Json(filtros);
        }

        [HttpGet]

        public async Task<IActionResult> BuscarAvance(int? grupo, string maestro, string nivel, string tipoCurso, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var cursos = await ObtenerCursos();

            // Filtros
            if (grupo.HasValue)
                cursos = cursos.Where(c => c.Id == grupo.Value).ToList();
            if (!string.IsNullOrEmpty(maestro))
                cursos = cursos.Where(c => c.Profesor == maestro).ToList();
            if (!string.IsNullOrEmpty(nivel))
                cursos = cursos.Where(c => c.Nivel == nivel).ToList();
            if (!string.IsNullOrEmpty(tipoCurso))
                cursos = cursos.Where(c => c.TipoCurso == tipoCurso).ToList();


            if (fechaDesde.HasValue)
            {
                var fechaDesdeUtc = fechaDesde.Value.Date;
                cursos = cursos.Where(c => c.FechaCreacion.Date >= fechaDesdeUtc).ToList();
            }

            if (fechaHasta.HasValue)
            {
                var fechaHastaUtc = fechaHasta.Value.Date.AddDays(1).AddTicks(-1);
                cursos = cursos.Where(c => c.FechaCreacion.Date <= fechaHastaUtc).ToList();
            }

            var avanceAlumnos = new List<AvanceAlumnoViewModel>();

            foreach (var curso in cursos)
            {
                var alumnos = await ObtenerAlumnosPorCurso(curso.Id);

                foreach (var alumno in alumnos)
                {
                    avanceAlumnos.Add(new AvanceAlumnoViewModel
                    {
                        Id = alumno.Id,
                        AlumnoNombre = $"{alumno.Nombre} {alumno.ApellidoPa} {alumno.ApellidoMa}",
                        GrupoNombre = curso.Nombre,
                        MaestroNombre = curso.Profesor,
                        Nivel = curso.Nivel,
                        TipoCurso = curso.TipoCurso,
                        FechaCreacion = curso.FechaCreacion
                    });
                }
            }

            return Json(avanceAlumnos);

        }
        public async Task<List<GrupoDetallesModel>> ObtenerCursos()
        {
            var cursos = new List<GrupoDetallesModel>();
            TipoGrupoModel tipoGrupo = new TipoGrupoModel();
            try
            {
                using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Curso";
                    using (SqlCommand cmd = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var curso = new GrupoDetallesModel
                                {
                                    Id = Convert.ToInt32(reader["id_cursos"]),
                                    Nombre = reader["nombre_curso"].ToString(),
                                    Nivel = tipoGrupo.Niveles(reader["id_nivel"].ToString()),
                                    TipoCurso = tipoGrupo.Tipo(reader["id_tipo_curso"].ToString()),
                                    FechaInicio = Convert.ToDateTime(reader["fecha_inicio"]),
                                    FechaFin = Convert.ToDateTime(reader["fecha_fin"]),
                                    Capacidad = Convert.ToInt32(reader["capacidad"]),
                                    FechaCreacion = Convert.ToDateTime(reader["fecha_creacion"]),
                                    Alumnos = new List<AlumnoModelView>()
                                };
                                cursos.Add(curso);
                            }
                        }
                    }
                }
                foreach (var curso in cursos)
                {
                    curso.Profesor = await ProfesorGrupo(curso.Id);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error en ObtenerCursos: " + ex.Message);
            }
            return cursos;
        }

        public async Task<string> ProfesorGrupo(int idcurso)
        {
            string nombre = "Sin Profe";
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT P.nombre_profesor FROM Profesores P INNER JOIN Avance_Alumnos AA ON P.id_profesor = AA.id_profesor WHERE AA.id_cursos = @Id_Curso;";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id_Curso", idcurso);
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            nombre = reader["nombre_profesor"].ToString();
                        }
                    }
                }
            }
            return nombre;
        }
        public async Task<List<AlumnoModel>> ObtenerAlumnosPorCurso(int idCurso)
        {
            var alumnos = new List<AlumnoModel>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = @"
                SELECT A.id_estudiantes, A.nombre_alumno, A.apellido_paterno, A.apellido_materno
                FROM Alumnos A
                INNER JOIN Avance_Alumnos AA ON A.id_estudiantes = AA.id_estudiantes
                WHERE AA.id_cursos = @Id_Curso;";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id_Curso", idCurso);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            alumnos.Add(new AlumnoModel
                            {
                                Id = Convert.ToInt32(reader["id_estudiantes"]),
                                Nombre = reader["nombre_alumno"].ToString(),
                                ApellidoPa = reader["apellido_paterno"].ToString(),
                                ApellidoMa = reader["apellido_materno"].ToString()
                            });
                        }
                    }
                }
            }
            return alumnos;
        }
        private async Task<List<string>> GetMaestros() => await Profesores();
        private List<string> GetNiveles() => new List<string>
            {
                "Introductorio",
                "Basico1",
                "Basico2",
                "Basico3",
                "Basico4",
                "Basico5",
                "Intermedio1",
                "Intermedio2",
                "Intermedio3",
                "Intermedio4",
                "Intermedio5",
                "Avanzado1",
                "Avanzado2",
                "Avanzado3",
                "Avanzado4",
                "Avanzado5",
                "FCE"
            };
        private List<string> GetTiposCurso() => new List<string> { "Semanal", "Sabatino", "Intensivo" };
        public async Task<List<string>> Profesores()
        {
            List<string> profesores = new List<string>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT nombre_profesor FROM Profesores;";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            profesores.Add(reader.GetString(0));
                        }
                    }
                }
            }
            return profesores;
        }
        public async Task<IActionResult> Graficas(int? grupo, string maestro, string nivel, string tipoCurso, DateTime? fechaDesde, DateTime? fechaHasta)
        {
            var cursos = await ObtenerCursos();

            if (grupo.HasValue)
                cursos = cursos.Where(c => c.Id == grupo.Value).ToList();
            if (!string.IsNullOrEmpty(maestro))
                cursos = cursos.Where(c => c.Profesor == maestro).ToList();
            if (!string.IsNullOrEmpty(nivel))
                cursos = cursos.Where(c => c.Nivel == nivel).ToList();
            if (!string.IsNullOrEmpty(tipoCurso))
                cursos = cursos.Where(c => c.TipoCurso == tipoCurso).ToList();
            if (fechaDesde.HasValue)
                cursos = cursos.Where(c => c.FechaCreacion.Date >= fechaDesde.Value.Date).ToList();
            if (fechaHasta.HasValue)
                cursos = cursos.Where(c => c.FechaCreacion.Date <= fechaHasta.Value.Date).ToList();

            var avanceAlumnos = new List<AvanceAlumnoViewModel>();
            foreach (var curso in cursos)
            {
                var alumnos = await ObtenerAlumnosPorCurso(curso.Id);
                foreach (var alumno in alumnos)
                {
                    avanceAlumnos.Add(new AvanceAlumnoViewModel
                    {
                        AlumnoNombre = $"{alumno.Nombre} {alumno.ApellidoPa} {alumno.ApellidoMa}",
                        GrupoNombre = curso.Nombre,
                        MaestroNombre = curso.Profesor,
                        Nivel = curso.Nivel,
                        TipoCurso = curso.TipoCurso,
                        FechaCreacion = curso.FechaCreacion
                    });
                }
            }

            ViewData["AvanceJson"] = System.Text.Json.JsonSerializer.Serialize(avanceAlumnos);

            return View("Graficas");
        }
        public async Task<IActionResult> GenerarHistorialVista(string busquedaId)
        {
            var alumnos = new List<AlumnoModel>();

            if (!string.IsNullOrWhiteSpace(busquedaId))
            {
                using (var connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    connection.Open();
                    var query = @"SELECT DISTINCT a.id_estudiantes, a.nombre_alumno, a.apellido_paterno, a.apellido_materno, a.matricula FROM Alumnos a INNER JOIN Historial_Avance_Alumnos h ON a.id_estudiantes = h.id_estudiantes WHERE a.id_estudiantes = @IdAlumno";

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

            return View(alumnos); 
        }
        [HttpGet]
        public async Task<IActionResult> GenerarHistorial(int idAlumno)
        {
            decimal calificaciontotaltotal = 0, asistenciaTotal = 0;
            var historiales = new List<HistorialAvanceAlumno>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Historial_Avance_Alumnos WHERE id_estudiantes = @Id_Estudiante";
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
                            avance.CalificacionTotal = (avance.CalContinua + avance.CalExMedia + avance.CalExFinal) / 30;
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
                                historial.Periodo = reader["fecha_inicio"].ToString() + " - " + reader["fecha_fin"].ToString();
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
    }

    public class AvanceAlumnoViewModel
    {
        public int Id { get; set; }
        public string AlumnoNombre { get; set; }
        public string GrupoNombre { get; set; }
        public string MaestroNombre { get; set; }
        public string Nivel { get; set; }
        public string TipoCurso { get; set; }
        public DateTime FechaCreacion { get; set; }
    }
}
