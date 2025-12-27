using Azure.Storage.Blobs;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AzureEventHubsBlobCheckpointLatency
{
    public class MainLatencyMeasurementFunction
    {
        public static readonly string StorageAccountUrl = Environment.GetEnvironmentVariable("STORAGE_ACCOUNT_URL")
                ?? "samainacclatencytestfc4a.blob.core.windows.net";

        public static readonly string RedisCacheUrl = Environment.GetEnvironmentVariable("REDIS_URL")
                ?? "rds-latency-cache-store.westeurope.redis.azure.net:10000";

        private readonly TelemetryClient _telemetryClient;
        private readonly ConfigurationOptions _redisConfig;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly ILogger<MainLatencyMeasurementFunction> _logger;

        public MainLatencyMeasurementFunction(
            ILogger<MainLatencyMeasurementFunction> logger,
            TelemetryClient telemetryClient,
            BlobServiceClient blobServiceClient,
            ConfigurationOptions redisConfig)
        {
            _logger = logger;
            _telemetryClient = telemetryClient;
            _blobServiceClient = blobServiceClient;
            _redisConfig = redisConfig;
        }

        [Function("MainLatencyMeasurementFunction")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processing a request.");

            var container = _blobServiceClient.GetBlobContainerClient("checkpoint");
            var blobClient = container.GetBlobClient("0");

            var blobDependencyTelemetry = new DependencyTelemetry
            {
                Name = "Azure Blob Storage",
                Type = "Azure Blob",
                Target = StorageAccountUrl,
                Data = $"checkpoint/0"
            };

            await CallWithDependencyTracking(() =>
            {
                return blobClient.SetMetadataAsync(new Dictionary<string, string>
                {
                    { "touched", DateTime.UtcNow.ToString() }
                });
            }, blobDependencyTelemetry);

            using var connectionMultiplexer = await ConnectionMultiplexer
                .ConnectAsync(_redisConfig, new StringWriter());

            var cache = connectionMultiplexer.GetDatabase();

            var redisDependencyTelemetry = new DependencyTelemetry
            {
                Name = "Redis Cache",
                Type = "Redis",
                Target = RedisCacheUrl,
                Data = "last-touched"
            };

            await CallWithDependencyTracking(() =>
            {
                return cache.StringSetAsync("last-touched", DateTime.UtcNow.ToString());
            }, redisDependencyTelemetry);

            return new OkObjectResult($"Operation Done {DateTime.UtcNow}");
        }

        private async Task CallWithDependencyTracking(Func<Task> action, DependencyTelemetry dependencyTelemetry)
        {
            dependencyTelemetry.Timestamp = DateTime.UtcNow;
            using (var operation = _telemetryClient.StartOperation(dependencyTelemetry))
            {
                try
                {
                    await action();
                }
                catch (Exception ex)
                {
                    operation.Telemetry.Success = false;
                    _telemetryClient.TrackException(ex);
                    throw;
                }
                finally
                {
                    _telemetryClient.StopOperation(operation);
                    _telemetryClient.Flush();
                }
            }
        }
    }
}