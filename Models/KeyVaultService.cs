namespace CelexWebApp.Models
{
    using Azure;
    using Azure.Identity;
    using Azure.Security.KeyVault.Secrets;
    using System;
    using System.Threading.Tasks;

    public class KeyVaultService
    {
        private readonly SecretClient _client;

        public KeyVaultService(string keyVaultUri)
        {
            _client = new SecretClient(new Uri(keyVaultUri), new DefaultAzureCredential());
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            var secret = await _client.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
    }
}
