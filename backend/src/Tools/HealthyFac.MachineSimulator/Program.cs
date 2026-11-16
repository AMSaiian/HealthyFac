using HealthyFac.MachineSimulator;
using HealthyFac.Shared.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDeploymentSettingsFile();
builder.Configuration.AddDotEnvFile();

builder.Services.AddSimulatorServices(builder.Configuration, builder.Environment);

var app = builder.Build();

app.UseSimulatorPipeline();
app.MapSimulatorEndpoints();

await app.RunAsync();

namespace HealthyFac.MachineSimulator
{
    public partial class Program;
}
