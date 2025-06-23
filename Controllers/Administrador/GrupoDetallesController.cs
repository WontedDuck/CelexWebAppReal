using CelexWebApp.Models;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using CelexWebApp.Models.ModelGrupos;
using CelexWebApp.Models.ProfesorMMV;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.Kiota.Abstractions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CelexWebApp.Controllers.Administrador
{
    public class GrupoDetallesController : Controller
    {
        GrupoDetallesModel grupo = new GrupoDetallesModel();
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public GrupoDetallesController(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Index(int id)
        {
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            string profesor = "";
            HttpContext.Session.SetString("id_curso", id.ToString());
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id);
            if (TempData["Informacion"] == "Informacion")
            {
                HttpContext.Session.SetString("id_profesor", extraerInformacion.ProfesorId());
                HttpContext.Session.SetString("profesor_nombre", await extraerInformacion.ProfesorNombre(profesor));
                profesor = HttpContext.Session.GetString("profesor_nombre");
            }
            grupo = await extraerInformacion.GrupoInfo(id, profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = (await extraerInformacion.AlumnosInfo())
                    .Select(alumno => new AlumnoModelView
                    {
                        alumno = alumno.alumno,
                        CalContinua = 0,
                        CalExMedia = 0,
                        CaliExFinal = 0,
                    }).ToList();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModelView>();
            }
            return View(grupo);
        }
        public async Task<IActionResult> BuscarProfesor(string tipoCurso)
        {
            int idtipo = 0;
            switch (tipoCurso)
            {
                case "Semanal":
                    idtipo = 1; break;
                case "Sabatino":
                    idtipo = 2; break;
                case "Intensivo":
                    idtipo = 3; break;
            }
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            return View(await extraerInformacion.BusquedaProfesor(idtipo));
        }
        public async Task<IActionResult> AgregarProfesor(int id, string nombre)
        {
            HttpContext.Session.SetString("id_profesor", id.ToString());
            HttpContext.Session.SetString("profesor_nombre", nombre.ToString());
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre);
            return View("Index", grupo);
        }
        public async Task<IActionResult> BuscarAlumnos()
        {
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            return View(await extraerInformacion.BusquedaAlumno());
        }
        [HttpPost]
        public async Task<IActionResult> AgregarAlumnos(List<int> alumnosSeleccionados)
        {
            if (alumnosSeleccionados == null || !alumnosSeleccionados.Any())
            {
                TempData["mensaje"] = "No se seleccionaron alumnos.";
                return RedirectToAction("BuscarAlumnos");
            }
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            int id_profesor = int.Parse(HttpContext.Session.GetString("id_profesor"));
            string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
            int capacidad = 0, ocupados = 0;
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT capacidad, COALESCE(ocupados, 0) AS ocupados FROM Curso WHERE id_cursos = @id_curso";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id_curso", id_curso);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            capacidad = Convert.ToInt32(reader.GetDecimal(0));
                            ocupados = Convert.ToInt32(reader.GetDecimal(1));
                        }
                    }
                }
            }
            int espacioDisponible = capacidad - ocupados;
            if (alumnosSeleccionados.Count > espacioDisponible)
            {
                TempData["mensaje"] = $"Solo hay {espacioDisponible} espacios disponibles.";
                return RedirectToAction("BuscarAlumnos");
            }
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                foreach (var id in alumnosSeleccionados)
                {
                    string query = "INSERT INTO Avance_Alumnos (id_estudiantes, id_cursos, id_profesor) VALUES (@id, @id_curso, @id_profesor)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id);
                        command.Parameters.AddWithValue("@id_curso", id_curso);
                        command.Parameters.AddWithValue("@id_profesor", id_profesor);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                string query2 = "UPDATE Curso SET ocupados = ISNULL(ocupados, 0) + @cantidad WHERE id_cursos = @id_curso;";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@cantidad", alumnosSeleccionados.Count);
                    command.Parameters.AddWithValue("@id_curso", id_curso);
                    await command.ExecuteNonQueryAsync();
                }
            }
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            if (TempData["Informacion"] == "Informacion")
            {
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = (await extraerInformacion.AlumnosInfo())
                    .Select(alumno => new AlumnoModelView
                    {
                        alumno = alumno.alumno,
                        CalContinua = 0,
                        CalExMedia = 0,
                        CaliExFinal = 0
                    }).ToList();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModelView>();
            }
            return View("Index", grupo);
        }
        public async Task<IActionResult> BajaAlumno(int id)
        {
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            int id_profesor = int.Parse(HttpContext.Session.GetString("id_profesor"));
            string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            await extraerInformacion.BorrarAlumno(id, id_curso);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            if (TempData["Informacion"] == "Informacion")
            {
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = (await extraerInformacion.AlumnosInfo())
                    .Select(alumno => new AlumnoModelView
                    {
                        alumno = alumno.alumno,
                        CalContinua = 0,
                        CalExMedia = 0,
                        CaliExFinal = 0
                    }).ToList();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModelView>();
            }
            return View("Index", grupo);
        }
        public async Task<IActionResult> Calificaciones(int id)
        {
            AlumnoModelView calificaciones = new AlumnoModelView();
            AlumnoModel alumno = new AlumnoModel();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Alumnos WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        if (reader.Read())
                        {
                            alumno.Id = int.Parse(reader["id_estudiantes"].ToString());
                            alumno.Nombre = reader["nombre_alumno"].ToString();
                            alumno.ApellidoPa = reader["apellido_paterno"].ToString();
                            alumno.ApellidoMa = reader["apellido_materno"].ToString();
                        }
                }
                string query2 = "SELECT calcontinua, calexmedia, caliexfinal FROM Avance_Alumnos WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        if (reader.Read())
                        {
                            calificaciones.CalContinua = reader["calcontinua"] != DBNull.Value ? Convert.ToInt32(reader["calcontinua"]) : 0;
                            calificaciones.CalExMedia = reader["calexmedia"] != DBNull.Value ? Convert.ToInt32(reader["calexmedia"]) : 0;
                            calificaciones.CaliExFinal = reader["caliexfinal"] != DBNull.Value ? Convert.ToInt32(reader["caliexfinal"]) : 0;
                        }
                }
            }
            calificaciones.alumno = alumno;
            return View(calificaciones);
        }
        [HttpPost]
        public async Task<IActionResult> ActualizarInformacion(AlumnoModelView alumnoModelView)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "UPDATE Avance_Alumnos SET calcontinua = @calcontinua, calexmedia = @calexmedia, caliexfinal = @caliexfinal WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@calcontinua", alumnoModelView.CalContinua);
                    command.Parameters.AddWithValue("@calexmedia", alumnoModelView.CalExMedia);
                    command.Parameters.AddWithValue("@caliexfinal", alumnoModelView.CaliExFinal);
                    command.Parameters.AddWithValue("@id", alumnoModelView.alumno.Id);
                    command.ExecuteNonQuery();
                }
            }
            return View("Calificaciones", alumnoModelView);
        }

        [HttpPost]
        public async Task<IActionResult> AgregarAsistencia(int id)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "UPDATE Avance_Alumnos SET asistencia = ISNULL(asistencia, 0) + 1 WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            int id_profesor = int.Parse(HttpContext.Session.GetString("id_profesor"));
            string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            if (TempData["Informacion"] == "Informacion")
            {
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = (await extraerInformacion.AlumnosInfo())
                    .Select(alumno => new AlumnoModelView
                    {
                        alumno = alumno.alumno,
                        CalContinua = 0,
                        CalExMedia = 0,
                        CaliExFinal = 0
                    }).ToList();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModelView>();
            }
            return View("Index", grupo);
        }
        [HttpPost]
        public async Task<IActionResult> QuitarAsistencia(int id)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "UPDATE Avance_Alumnos SET asistencia = ISNULL(asistencia, 0) - 1 WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    command.ExecuteNonQuery();
                }
            }
            int id_curso = int.Parse(HttpContext.Session.GetString("id_curso"));
            int id_profesor = int.Parse(HttpContext.Session.GetString("id_profesor")); 
            string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(id_curso);
            if (TempData["Informacion"] == "Informacion")
            {
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
            }
            grupo = await extraerInformacion.GrupoInfo(id_curso, nombre_profesor);
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = (await extraerInformacion.AlumnosInfo())
                    .Select(alumno => new AlumnoModelView
                    {
                        alumno = alumno.alumno,
                        CalContinua = 0,
                        CalExMedia = 0,
                        CaliExFinal = 0
                    }).ToList();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModelView>();
            }
            return View("Index", grupo);
        }
        [HttpPost]
        public async Task<IActionResult> BorrarGrupo(int IdGrupo)
        {
            using(SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Avance_Alumnos WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", IdGrupo);
                    await command.ExecuteNonQueryAsync();
                }
                string query2 = "DELETE FROM Curso WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id", IdGrupo);
                    await command.ExecuteNonQueryAsync();
                }
            }
            return RedirectToAction("Index", "Grupos");
        }
        [HttpPost]
        public async Task<IActionResult> GuardarGrupo(int IdGrupo, string Nombre, string Nivel, string TipoCurso, DateTime FechaInicio, DateTime FechaFin, int Capacidad, int Ocupados, string accion)
        {
            int[] id_borrar = new int[Capacidad];
            int i = 0, e = 0, idnivel = 0, idtipo = 0;
            switch (Nivel)
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
            }
            switch (TipoCurso)
            {
                case "Semanal":
                    idtipo = 1; break;
                case "Sabatino":
                    idtipo = 2; break;
                case "Intensivo":
                    idtipo = 3; break;
            }
            if (accion == "modificar")
            {
                if (Capacidad < Ocupados)
                {
                    TempData["mensaje"] = "La capacidad no puede ser menor a los ocupados.";
                }
                else if (Capacidad <= 0)
                {
                    TempData["mensaje"] = "La capacidad debe ser mayor a 0.";
                }
                else if (FechaInicio >= FechaFin)
                {
                    TempData["mensaje"] = "La fecha de inicio no puede ser mayor o igual a la fecha de fin.";
                }
                else if (string.IsNullOrEmpty(Nombre) || string.IsNullOrEmpty(Nivel) || string.IsNullOrEmpty(TipoCurso))
                {
                    TempData["mensaje"] = "Todos los campos son obligatorios.";
                }
                else if (FechaInicio <= FechaFin.AddMonths(-2))
                {
                    TempData["mensaje"] = "La fecha debe ser al menos dos meses antes de la final";
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                    {
                        await connection.OpenAsync();
                        string query = "UPDATE Curso SET nombre_curso = @Nombre, id_nivel = @Nivel, id_tipo_curso = @TipoCurso, fecha_inicio = @FechaInicio, fecha_fin = @FechaFin, capacidad = @Capacidad, ocupados = @Ocupados WHERE id_cursos = @IdGrupo";
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@Nombre", Nombre);
                            command.Parameters.AddWithValue("@Nivel", idnivel);
                            command.Parameters.AddWithValue("@TipoCurso", idtipo);
                            command.Parameters.AddWithValue("@FechaInicio", FechaInicio);
                            command.Parameters.AddWithValue("@FechaFin", FechaFin);
                            command.Parameters.AddWithValue("@Capacidad", Capacidad);
                            command.Parameters.AddWithValue("@Ocupados", Ocupados);
                            command.Parameters.AddWithValue("@IdGrupo", IdGrupo);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                    TempData["mensaje"] = "Se ejecutó el bloque MODIFICAR.";
                }
            }
            else if (accion == "terminar")
            {
                int id_prof = 0;
                var InfoAlumnos = new List<HistorialAvanceAlumno>();
                using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
                {
                    await connection.OpenAsync();
                    string query = "SELECT * FROM Avance_Alumnos WHERE id_cursos = @Id_cursos";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id_cursos", IdGrupo);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (reader.Read())
                            {
                                int id_estudiantes = reader.GetInt16(0);
                                id_prof = reader.GetInt16(1);
                                int id_cursos = reader.GetInt16(2);
                                if (reader.IsDBNull(3) || reader.IsDBNull(4) || reader.IsDBNull(5) || reader.IsDBNull(6))
                                {
                                    TempData["error"] = "Alumnos sin algun valor (asistencia, calificacion; continua, media o final) no se registraron en historial";
                                    continue;
                                }
                                id_borrar[i] = id_estudiantes;
                                i++;
                                InfoAlumnos.Add(new HistorialAvanceAlumno
                                {
                                    IdEstudiante = reader.GetInt16(0),
                                    IdProfesor = reader.GetInt16(1),
                                    IdCurso = reader.GetInt16(2),
                                    Asistencia = reader.GetDecimal(3),
                                    CalContinua = reader.GetDecimal(4),
                                    CalExMedia = reader.GetDecimal(5),
                                    CalExFinal = reader.GetDecimal(6)
                                });
                            }
                        }
                    }
                    e = Ocupados - i;
                    int ocupads = 0;
                    bool registrohistorialcurso = false;
                    foreach (var dato in InfoAlumnos)
                    {
                        string insertQuery = "INSERT INTO Historial_Avance_Alumnos (id_estudiantes, id_profesor, id_cursos, asistencia, calcontinua, calexmedia, caliexfinal) VALUES (@Id_estudiantes, @Id_profesor, @Id_cursos, @Asistencia, @Calcontinua, @Calexmedia, @Caliexfinal)";
                        using (SqlCommand insertCmd = new SqlCommand(insertQuery, connection))
                        {
                            insertCmd.Parameters.AddWithValue("@Id_estudiantes", dato.IdEstudiante);
                            insertCmd.Parameters.AddWithValue("@Id_profesor", dato.IdProfesor);
                            insertCmd.Parameters.AddWithValue("@Id_cursos", dato.IdCurso);
                            insertCmd.Parameters.AddWithValue("@Asistencia", dato.Asistencia);
                            insertCmd.Parameters.AddWithValue("@Calcontinua", dato.CalContinua);
                            insertCmd.Parameters.AddWithValue("@Calexmedia", dato.CalExMedia);
                            insertCmd.Parameters.AddWithValue("@Caliexfinal", dato.CalExFinal);
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }
                    string selectquery = "SELECT * FROM Historial_Curso WHERE id_curso_origen = @Id_curso_origen AND id_nivel = @Id_nivel AND id_tipo_curso = @Id_tipo_curso AND fecha_inicio >= @Fecha_inicio_minima AND fecha_fin <= @Fecha_fin_maxima;";

                    using (SqlCommand select = new SqlCommand(selectquery, connection))
                    {
                        select.Parameters.AddWithValue("@Id_curso_origen", IdGrupo);
                        select.Parameters.AddWithValue("@Id_nivel", idnivel);
                        select.Parameters.AddWithValue("@Id_tipo_curso", idtipo);
                        select.Parameters.AddWithValue("@Fecha_inicio_minima", FechaInicio);
                        select.Parameters.AddWithValue("@Fecha_fin_maxima", FechaFin);
                        using (SqlDataReader reader = await select.ExecuteReaderAsync())
                            if (reader.Read())
                            {
                                registrohistorialcurso = true;
                                ocupads = Convert.ToInt32(reader.GetDecimal(7));
                            }
                    }
                    if (registrohistorialcurso)
                    {
                        string updatequery = "UPDATE Historial_Curso SET ocupados = @Ocupados WHERE id_curso_origen = @Id_curso_origen AND id_nivel = @Id_nivel AND id_tipo_curso = @Id_tipo_curso AND fecha_inicio >= @Fecha_inicio_minima AND fecha_fin <= @Fecha_fin_maxima;";
                        using (SqlCommand updateCmd = new SqlCommand(updatequery, connection))
                        {
                            updateCmd.Parameters.AddWithValue("@Ocupados", ocupads + i);
                            updateCmd.Parameters.AddWithValue("@Id_curso_origen", IdGrupo);
                            updateCmd.Parameters.AddWithValue("@Id_nivel", idnivel);
                            updateCmd.Parameters.AddWithValue("@Id_tipo_curso", idtipo);
                            updateCmd.Parameters.AddWithValue("@Fecha_inicio_minima", FechaInicio);
                            updateCmd.Parameters.AddWithValue("@Fecha_fin_maxima", FechaFin);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                    }
                    else
                    {
                        string insert3query = "INSERT INTO Historial_Curso (id_curso_origen, id_nivel, id_tipo_curso, fecha_inicio, fecha_fin, capacidad, ocupados, nombre_curso, id_profesor) VALUES (@Id_curso_origen, @Id_nivel, @Id_tipo_curso, @Fecha_inicio, @Fecha_fin, @Capacidad, @Ocupados, @Nombre_curso, @Id_profesor)";
                        using (SqlCommand insert3Cmd = new SqlCommand(insert3query, connection))
                        {
                            insert3Cmd.Parameters.AddWithValue("@Id_curso_origen", IdGrupo);
                            insert3Cmd.Parameters.AddWithValue("@Id_nivel", idnivel);
                            insert3Cmd.Parameters.AddWithValue("@Id_tipo_curso", idtipo);
                            insert3Cmd.Parameters.AddWithValue("@Fecha_inicio", FechaInicio);
                            insert3Cmd.Parameters.AddWithValue("@Fecha_fin", FechaFin);
                            insert3Cmd.Parameters.AddWithValue("@Capacidad", Capacidad);
                            insert3Cmd.Parameters.AddWithValue("@Ocupados", i);
                            insert3Cmd.Parameters.AddWithValue("@Nombre_curso", Nombre);
                            insert3Cmd.Parameters.AddWithValue("@Id_profesor", id_prof);
                            await insert3Cmd.ExecuteNonQueryAsync();
                        }
                    }
                    while (0 <= i)
                    {
                        string query4 = "DELETE FROM Avance_Alumnos WHERE id_estudiantes = @Id_estudiantes";
                        using (SqlCommand command = new SqlCommand(query4, connection))
                        {
                            command.Parameters.AddWithValue("@Id_estudiantes", id_borrar[i]);
                            await command.ExecuteNonQueryAsync();
                        }
                        i--;
                    }
                    string query5 = "UPDATE Curso SET ocupados = @Ocupados WHERE id_cursos = @Id_cursos";
                    using (SqlCommand command = new SqlCommand(query5, connection))
                    {
                        command.Parameters.AddWithValue("@Ocupados", e);
                        command.Parameters.AddWithValue("@Id_cursos", IdGrupo);
                        await command.ExecuteNonQueryAsync();
                    }
                }
                TempData["mensaje"] = "Se ejecutó el bloque TERMINAR.";
            }
            ExtraerInformacion extraerInformacion = new ExtraerInformacion(_logger, _graphServiceClient, _downstreamApi, _conexion);
            TempData["Informacion"] = await extraerInformacion.EstadoGrupo(IdGrupo);
            if (TempData["Informacion"] == "Informacion")
            {
                string nombre_profesor = HttpContext.Session.GetString("profesor_nombre");
                nombre_profesor = await extraerInformacion.ProfesorNombre(nombre_profesor);
                grupo = await extraerInformacion.GrupoInfo(IdGrupo, nombre_profesor);
            }
            if (TempData["Informacion"] == "Informacion")
            {
                grupo.Alumnos = (await extraerInformacion.AlumnosInfo())
                    .Select(alumno => new AlumnoModelView
                    {
                        alumno = alumno.alumno,
                        CalContinua = 0,
                        CalExMedia = 0,
                        CaliExFinal = 0
                    }).ToList();
            }
            else
            {
                grupo.Alumnos = new List<AlumnoModelView>();
            }
            return View("Index", grupo);
        }
    }
}
