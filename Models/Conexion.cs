namespace CelexWebApp.Models
{
    public class Conexion
    {
        private readonly KeyVaultService _keyVaultService;
        private readonly string _keyVaultSecretName = "MiBdContrasena";

        public Conexion(KeyVaultService keyVaultService)
        {
            _keyVaultService = keyVaultService;
        }

        public async Task<string> GetConexionAsync()
        {
            string password = await _keyVaultService.GetSecretAsync(_keyVaultSecretName);
            return $"Data Source=celex-sql-server.database.windows.net;Initial Catalog=DBCelex;Persist Security Info=True;User ID=celexadmin;Password={password};Trust Server Certificate=True";
        }
    }
}
