using HealthyFac.Insights.Application;
using HealthyFac.Insights.Infrastructure;
using HealthyFac.Insights.Infrastructure.Database.Initialization;
using HealthyFac.Insights.Web;
using HealthyFac.Shared.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddSeedDataFile(builder.Environment);
builder.Configuration.AddDotEnvFile();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseApiPipeline();
app.MapApiEndpoints();

await app.RunAsync();

namespace HealthyFac.Insights.Web
{
    public partial class Program;
}
