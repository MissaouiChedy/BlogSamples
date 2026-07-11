using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AsyncProcessingApp.Configuration;
using AsyncProcessingApp.Observability;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection;

var azureCredentialOptions = new DefaultAzureCredentialOptions
{
    /*
     * Only include AZ CLI and Managed Identity credentials
     */
    ExcludeAzureCliCredential = false,
    ExcludeManagedIdentityCredential = false,

    ExcludeAzureDeveloperCliCredential = true,
    ExcludeAzurePowerShellCredential = true,
    ExcludeEnvironmentCredential = true,
    ExcludeBrokerCredential = true,
    ExcludeInteractiveBrowserCredential = true,
    ExcludeVisualStudioCodeCredential = true,
    ExcludeVisualStudioCredential = true,
    ExcludeWorkloadIdentityCredential = true
};

var azureCredentials = new DefaultAzureCredential(azureCredentialOptions);

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();


/*
 * Configure Storage Blob Configuration to be loaded as a Strongly typed Entity
 */
builder.Services
    .AddOptions<StorageBlobConfiguration>()
    .Bind(builder.Configuration)
    .ValidateDataAnnotations()
    .ValidateOnStart();

/*
 * Reset the default logging filter for Application Insights
 */
builder.Services.Configure<LoggerFilterOptions>(options =>
{
    var defaultRule = options.Rules.FirstOrDefault(rule =>
        rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

    if (defaultRule is not null)
    {
        options.Rules.Remove(defaultRule);
    }
});

builder.Services.AddSingleton(serviceProvider =>
{
    var blobConfiguration = serviceProvider
        .GetRequiredService<IOptions<StorageBlobConfiguration>>().Value;

    var blobServiceClient = new BlobServiceClient(
            new Uri(blobConfiguration.StorageAccountUrl!),
            azureCredentials
        );

    return blobServiceClient
        .GetBlobContainerClient(blobConfiguration.EnrichedEventsContainer);
});

builder.Services.AddSingleton<MetricsTracker>();

builder.Build().Run();
