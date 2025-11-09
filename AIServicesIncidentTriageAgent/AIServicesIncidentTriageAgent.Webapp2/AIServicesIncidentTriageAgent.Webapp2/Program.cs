using AIServicesIncidentTriageAgent.Webapp2.Components.Shared.Services;
using AIServicesIncidentTriageAgent.Webapp2.Client.Pages;
using AIServicesIncidentTriageAgent.Webapp2.Components;
using AIServicesIncidentTriageAgent.Webapp2.Components.Shared;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddSingleton<ITicketService, CosmosDbTicketService>();
builder.Services.AddSingleton<IResolutionService, CosmosDbResolutionService>();
builder.Services.AddSingleton<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddSingleton<IAzureOpenAIService, AzureOpenAIService>();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(AIServicesIncidentTriageAgent.Webapp2.Client._Imports).Assembly);

app.Run();
