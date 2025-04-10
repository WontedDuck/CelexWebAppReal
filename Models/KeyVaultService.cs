using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace CelexWebApp.Models
{
    public class KeyVaultService
    {
        private readonly SecretClient _client;

        public KeyVaultService(string keyVaultUri, IConfiguration configuration)
        {
            var credential = new ClientSecretCredential(
                configuration["AzureAd:TenantId"],
                configuration["AzureAd:ClientId"],
                configuration["AzureAd:ClientSecret"]
            );

            _client = new SecretClient(new Uri(keyVaultUri), credential);
        }

        public async Task<string> GetSecretAsync(string secretName)
        {
            var secret = await _client.GetSecretAsync(secretName);
            return secret.Value.Value;
        }
    }
}
