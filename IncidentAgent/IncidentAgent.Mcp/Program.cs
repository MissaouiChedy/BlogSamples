using IncidentAgent.Mcp.Tools;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<CosmosDBConfiguration>(
    builder.Configuration.GetSection("CosmosDb"));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddOpenApi();

builder.Services.AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithToolsFromAssembly();

builder.Services.AddScoped<KnowledgeBaseManager>();
builder.Services.AddScoped<KnowledgeBaseRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapGet("/api/healthz", () => Results.Ok("Healthy"));

app.MapMcp("/api/mcp");

await app.RunAsync();

