namespace CelexWebApp.Models.AdministradorMMV
{
    public class TipoGrupoModel
    {
        public string Niveles(string nivel)
        {
            string nivelcurso = "";
            switch (nivel)
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
            return nivelcurso;
        }
        public string Tipo(string tipoid)
        {
            string tipocurso = "";
            switch (tipoid)
            {
                case "1":
                    tipocurso = "Semanal"; break;
                case "2":
                    tipocurso = "Sabatino"; break;
                case "3":
                    tipocurso = "Intensivo"; break;
            }
            return tipocurso;
        }
    }
}
