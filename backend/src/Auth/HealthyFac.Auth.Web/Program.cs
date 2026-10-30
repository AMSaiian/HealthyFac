using HealthyFac.Auth.Application;
using HealthyFac.Auth.Infrastructure;
using HealthyFac.Auth.Infrastructure.Database;
using HealthyFac.Auth.Web;
using HealthyFac.Shared.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddDotEnvFile();

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration, builder.Environment);
builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseApiPipeline();

await app.RunAsync();
