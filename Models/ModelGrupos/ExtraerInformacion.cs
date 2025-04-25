using CelexWebApp.Controllers;
using CelexWebApp.Models.AdministradorMMV;
using CelexWebApp.Models.AlumnoMMV;
using CelexWebApp.Models.ProfesorMMV;
using Microsoft.Data.SqlClient;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Abstractions;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace CelexWebApp.Models.ModelGrupos
{
    public class ExtraerInformacion
    {
        public int id_profesor = 0;
        public int[] id_alumno;
        private readonly IDownstreamApi _downstreamApi;
        private readonly GraphServiceClient _graphServiceClient;
        private readonly ILogger<HomeController> _logger;
        private readonly Conexion _conexion;

        public ExtraerInformacion(ILogger<HomeController> logger, GraphServiceClient graphServiceClient, IDownstreamApi downstreamApi, Conexion conexion)
        {
            _logger = logger;
            _graphServiceClient = graphServiceClient; ;
            _downstreamApi = downstreamApi; ;
            _conexion = conexion;
        }

        [AuthorizeForScopes(ScopeKeySection = "MicrosoftGraph:Scopes")]
        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<string> EstadoGrupo(int id_curso)
        {
            string estado = "";
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query1 = "SELECT * FROM Avance_Alumnos WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query1, connection))
                {
                    command.Parameters.AddWithValue("@id", id_curso);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            estado = "Informacion";
                            id_profesor = int.Parse(reader["id_profesor"].ToString());
                        }
                        else
                        { 
                            estado = "Grupo Vacio"; 
                        }
                    }
                }
                if (estado == "Informacion")
                {
                    id_alumno = new int[await NumeroAlumnos(id_curso)];
                    string query2 = "SELECT id_estudiantes FROM Avance_Alumnos WHERE id_cursos = @id";
                    using (SqlCommand command = new SqlCommand(query2, connection))
                    {
                        command.Parameters.AddWithValue("@id", id_curso);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            int i = 0;
                            while (await reader.ReadAsync())
                            {
                                id_alumno[i] = int.Parse(reader["id_estudiantes"].ToString());
                                i++;
                            }
                        }
                    }
                }
            }
            
            return estado;
        }
        public async Task<int> NumeroAlumnos(int id)
        {
            int numero = 0;
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT COUNT(*) FROM Avance_Alumnos WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    numero = (int)await command.ExecuteScalarAsync();
                }
            }
            return numero;
        }

        public async Task<List<ProfesorModel>> BusquedaProfesor()
        {
            var profesores = new List<ProfesorModel>();

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
                            profesores.Add(new ProfesorModel
                            {
                                Id = int.Parse(reader["id_profesor"].ToString()),
                                Numero_Empleado = int.Parse(reader["numero_empleado"].ToString()),
                                Nombre = reader["nombre_profesor"].ToString(),
                                Telefono = reader["telefono_profesor"].ToString(),
                            });
                        }
                    }
                }
            }
            return profesores;
        }
        public async Task<List<AlumnoModel>> BusquedaAlumno()
        {
            var alumnos = new List<AlumnoModel>();

            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT id_estudiantes, nombre_alumno, apellido_paterno, apellido_materno FROM Alumnos WHERE id_estudiantes NOT IN (SELECT id_estudiantes FROM Avance_Alumnos);";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            alumnos.Add(new AlumnoModel
                            {
                                Id = int.Parse(reader["id_estudiantes"].ToString()),
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
        public async Task<string> ProfesorNombre(string nombre_profesor)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Profesores WHERE id_profesor = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id_profesor);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            nombre_profesor = reader["nombre_profesor"].ToString();
                        }
                    }
                }
            }
            return nombre_profesor;
        }
        public string ProfesorId()
        {
            return id_profesor.ToString();
        }
        public async Task<List<AlumnoModel>> AlumnosInfo()
        {
            var alumnos = new List<AlumnoModel>();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Alumnos WHERE id_estudiantes = @id";
                for (int i = 0; i < id_alumno.Length; i++)
                {
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@id", id_alumno[i]);
                        using (SqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if(await reader.ReadAsync())
                            {
                                alumnos.Add(new AlumnoModel
                                {
                                    Id = int.Parse(reader["id_estudiantes"].ToString()),
                                    Nombre = reader["nombre_alumno"].ToString(),
                                    ApellidoPa = reader["apellido_paterno"].ToString(),
                                    ApellidoMa = reader["apellido_materno"].ToString()
                                });
                            }
                        }
                    }
                }
            }
            return alumnos;
        }
        public async Task<GrupoDetallesModel> GrupoInfo(int id, string nombre_profesor)
        {
            var grupo = new GrupoDetallesModel();
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "SELECT * FROM Curso WHERE id_cursos = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id);
                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            grupo = new GrupoDetallesModel()
                            {
                                Profesor = nombre_profesor,
                                Nombre = reader["nombre_curso"].ToString(),
                                Nivel = reader["id_nivel"].ToString(),
                                TipoCurso = reader["id_tipo_curso"].ToString(),
                                FechaInicio = DateTime.Parse(reader["fecha_inicio"].ToString()),
                                FechaFin = DateTime.Parse(reader["fecha_fin"].ToString()),
                                Capacidad = int.Parse(reader["capacidad"].ToString()),
                                Ocupados = reader["ocupados"] != DBNull.Value ? int.Parse(reader["ocupados"].ToString()) : 0
                            };
                        }
                    }
                }
                
            }
            return grupo;
        }
        public async Task BorrarAlumno(int id_alumno, int id_curso)
        {
            using (SqlConnection connection = new SqlConnection(await _conexion.GetConexionAsync()))
            {
                await connection.OpenAsync();
                string query = "DELETE FROM Avance_Alumnos WHERE id_estudiantes = @id";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@id", id_alumno);
                    await command.ExecuteNonQueryAsync();
                }
                string query2 = "UPDATE Curso SET ocupados = ISNULL(ocupados, 0) - 1 WHERE id_cursos = @id_curso;";
                using (SqlCommand command = new SqlCommand(query2, connection))
                {
                    command.Parameters.AddWithValue("@id_curso", id_curso);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
    }
}
