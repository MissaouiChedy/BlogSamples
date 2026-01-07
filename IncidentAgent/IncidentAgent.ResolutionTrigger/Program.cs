using IncidentAgent.ResolutionTrigger;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services
    .AddOptions<AgentConfiguration>()
    .Configure<IConfiguration>((settings, configuration) =>
    {
        configuration
            .GetSection("AzureOpenAI")
            .Bind(settings);
    })
    .ValidateDataAnnotations();

await builder
    .Build().RunAsync();
