using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Common_Shared_Services.Interfaces;
using Common_Shared_Services.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Common_Shared_Services.Extensions
{
    public static class RedisCacheServiceExtensions
    {
        /// <summary>
        /// Registers IConnectionMultiplexer and ICacheService, pulling Redis connection info from Key Vault.
        /// Expects the following in IConfiguration:
        ///   - "KeyVault:Uri"        (the URI of your Key Vault, e.g. "https://my-vault.vault.azure.net/")
        ///   - "KeyVault:TenantId"   (optional if using Managed Identity)
        ///   - "KeyVault:ClientId"   (optional if using a client credential)
        ///   - "KeyVault:ClientSecret" (optional, see notes below)
        ///   - Secrets named "Redis--Endpoint" and "Redis--Password" in Key Vault
        /// </summary>
        public static IServiceCollection AddRedisCacheService(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Build a SecretClient for Key Vault
            var vaultUri = configuration["KeyVault:Uri"];
            if (string.IsNullOrWhiteSpace(vaultUri))
            {
                throw new InvalidOperationException("Configuration must contain 'KeyVault:Uri'.");
            }

            // Use DefaultAzureCredential (supports Managed Identity, environment, VS credential, etc.)
            var credential = new DefaultAzureCredential();
            var secretClient = new SecretClient(new Uri(vaultUri), credential);

            // 2. Fetch the two secrets: "Redis--Endpoint" and "Redis--Password"
            KeyVaultSecret endpointSecret, passwordSecret;
            try
            {
                endpointSecret = secretClient.GetSecret("Redis--Endpoint");
                passwordSecret = secretClient.GetSecret("Redis--Password");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Unable to retrieve Redis secrets from Key Vault.", ex);
            }

            var redisEndpoint = endpointSecret.Value;
            var redisPassword = passwordSecret.Value;

            if (string.IsNullOrEmpty(redisEndpoint) || string.IsNullOrEmpty(redisPassword))
            {
                throw new InvalidOperationException("Redis endpoint or password is empty in Key Vault secrets.");
            }

            // 3. Register IConnectionMultiplexer as a singleton
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configString = $"{redisEndpoint},password={redisPassword}";
                return ConnectionMultiplexer.Connect(configString);
            });

            // 4. Register ICacheService
            services.AddSingleton<ICacheService, CacheService>();

            return services;
        }
    }
}
