using Azure.Data.Tables;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using PollStar.Core.Configuration;

namespace PollStar.Polls.HealthCheck;

    public class StorageAccountHealthCheck : IHealthCheck
    {
        private const string TableName = "polls";
        private readonly IOptions<AzureConfiguration> _configOptions;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new())
        {
            
            var config = _configOptions.Value;
            try
            {
                var storageUri = new Uri($"https://{config.StorageAccount}.table.core.windows.net");
                var tableClient = new TableClient(
                    storageUri,
                    "connectivitytest",
                    new TableSharedKeyCredential(config.StorageAccount, config.StorageKey));
                await tableClient.CreateIfNotExistsAsync(cancellationToken);
                await tableClient.DeleteAsync(cancellationToken);
                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy();
            }
        }

        public StorageAccountHealthCheck(IOptions<AzureConfiguration> configOptions)
        {
            _configOptions = configOptions;
        }
    }
