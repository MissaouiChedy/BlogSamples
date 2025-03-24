using Azure.Identity;
using Azure.Storage.Blobs;
using AzureEventHubsBlobCheckpointLatency;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

var builder = FunctionsApplication.CreateBuilder(args);

var mainIdentity = new DefaultAzureCredential();

builder
    .ConfigureFunctionsWebApplication();

builder.Services.AddApplicationInsightsTelemetryWorkerService();

var blobServiceClient = new BlobServiceClient(
    new Uri($"https://{MainLatencyMeasurementFunction.StorageAccountName}"),
    mainIdentity);

builder
    .Services
    .AddSingleton(blobServiceClient);

var configurationOptions = ConfigurationOptions
    .Parse($"{MainLatencyMeasurementFunction.RedisCacheName}:6380");

configurationOptions = await configurationOptions
    .ConfigureForAzureWithTokenCredentialAsync(mainIdentity);

configurationOptions.AbortOnConnectFail = false;

builder.Services.AddSingleton((provider) => configurationOptions);

builder.Build().Run();
