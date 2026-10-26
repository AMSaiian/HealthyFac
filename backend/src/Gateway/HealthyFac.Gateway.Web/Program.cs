using HealthyFac.Gateway.Web;
using HealthyFac.Shared.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile();

builder.Services.AddGatewayServices(builder.Configuration);

var app = builder.Build();

app.UseGatewayPipeline();
app.MapGatewayEndpoints();

await app.RunAsync();

namespace HealthyFac.Gateway.Web
{
    public partial class Program;
}
