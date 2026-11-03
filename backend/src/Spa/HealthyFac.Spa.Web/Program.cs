using HealthyFac.Shared.Web.Configuration;
using HealthyFac.Spa.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile();

builder.Services.AddSpaServices(builder.Configuration);

var app = builder.Build();

app.UseSpaPipeline();
app.MapSpaEndpoints();

await app.RunAsync();

namespace HealthyFac.Spa.Web
{
    public partial class Program;
}
