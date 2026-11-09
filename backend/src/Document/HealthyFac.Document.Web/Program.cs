using HealthyFac.Document.Application;
using HealthyFac.Document.Infrastructure;
using HealthyFac.Document.Infrastructure.Database.Initialization;
using HealthyFac.Document.Web;
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

namespace HealthyFac.Document.Web
{
    public partial class Program;
}
