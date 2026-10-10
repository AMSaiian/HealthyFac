using HealthyFac.Core.Application;
using HealthyFac.Core.Infrastructure;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Core.Web;
using HealthyFac.Shared.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration, builder.Environment);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseApiPipeline();
app.MapApiEndpoints();

await app.RunAsync();

namespace HealthyFac.Core.Web
{
    public partial class Program;
}
