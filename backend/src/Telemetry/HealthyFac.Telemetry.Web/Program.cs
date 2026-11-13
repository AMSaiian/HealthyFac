using HealthyFac.Shared.Web.Configuration;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Infrastructure;
using HealthyFac.Telemetry.Infrastructure.Database.Initialization;
using HealthyFac.Telemetry.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSeedDataFile(builder.Environment);
builder.Configuration.AddDeploymentSettingsFile();
builder.Configuration.AddDotEnvFile();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseApiPipeline();
app.MapApiEndpoints();

await app.RunAsync();

namespace HealthyFac.Telemetry.Web
{
    public partial class Program;
}
