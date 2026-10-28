using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using HealthyFac.Shared.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.Web.HealthChecks;

/// <summary>
/// Provides extension methods for registering the health checks and serving them on the health port.
/// </summary>
public static class HealthCheckExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the health check service and the validated port of the health endpoints. Each service appends
        /// its own checks. Tag a check with <see cref="HealthCheckConstants.ReadyTag"/> to include it in readiness.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/> for registering individual checks.</returns>
        public IHealthChecksBuilder AddSharedHealthChecks(IConfiguration configuration)
        {
            services.AddValidatedOptions<HealthEndpointOptions, HealthEndpointOptionsValidator>(
                HealthEndpointOptions.SectionName,
                configuration);

            return services.AddHealthChecks();
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>
        /// Answers three health endpoints on the health port only. Liveness runs no checks and always answers 200.
        /// Readiness runs the checks tagged ready. The detailed endpoint runs every check and reports each result.
        /// </summary>
        /// <remarks>
        /// Call it as the first middleware. Health requests then skip the HTTPS redirect, request logging, CORS,
        /// authentication, and authorization. The middleware compares the local port of the connection, which a client
        /// cannot change. It never reads the <c>Host</c> header, because a client can send any value. The platform
        /// must not route the health port publicly. The detailed endpoint writes the exception message of each check.
        /// That text can show internal host names and ports.
        /// </remarks>
        /// <returns>The <see cref="IApplicationBuilder"/> instance for chained configuration.</returns>
        public IApplicationBuilder UseSharedHealthChecks()
        {
            var port = app.ApplicationServices.GetRequiredService<IOptions<HealthEndpointOptions>>().Value.Port;

            app.UseHealthChecks(
                HealthCheckConstants.Routes.Liveness,
                port,
                new HealthCheckOptions
                {
                    Predicate = _ => false,
                    ResponseWriter = WriteLivenessAsync,
                });

            app.UseHealthChecks(
                HealthCheckConstants.Routes.Readiness,
                port,
                new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains(HealthCheckConstants.ReadyTag),
                    ResponseWriter = WriteLivenessAsync,
                });

            app.UseHealthChecks(
                HealthCheckConstants.Routes.Detailed,
                port,
                new HealthCheckOptions
                {
                    Predicate = _ => true,
                    ResponseWriter = WriteDetailedAsync,
                });

            return app;
        }
    }

    private static Task WriteLivenessAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = MediaTypeNames.Text.Plain;

        return context.Response.WriteAsync(report.Status.ToString(), context.RequestAborted);
    }

    private static async Task WriteDetailedAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = $"{MediaTypeNames.Application.Json}; charset=utf-8";

        using var stream = new MemoryStream();
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false });

        writer.WriteStartObject();
        writer.WriteString(HealthCheckConstants.Properties.Status, report.Status.ToString());
        writer.WriteNumber(HealthCheckConstants.Properties.TotalDuration, report.TotalDuration.TotalMilliseconds);

        writer.WriteStartArray(HealthCheckConstants.Properties.Checks);
        foreach (var (name, entry) in report.Entries)
        {
            writer.WriteStartObject();
            writer.WriteString(HealthCheckConstants.Properties.Name, name);
            writer.WriteString(HealthCheckConstants.Properties.Status, entry.Status.ToString());
            writer.WriteNumber(HealthCheckConstants.Properties.Duration, entry.Duration.TotalMilliseconds);

            if (!string.IsNullOrWhiteSpace(entry.Description))
            {
                writer.WriteString(HealthCheckConstants.Properties.Description, entry.Description);
            }

            if (entry.Exception is not null)
            {
                writer.WriteString(HealthCheckConstants.Properties.Error, entry.Exception.Message);
            }

            writer.WriteStartArray(HealthCheckConstants.Properties.Tags);
            foreach (var tag in entry.Tags)
            {
                writer.WriteStringValue(tag);
            }

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
        await writer.FlushAsync(context.RequestAborted);

        await context.Response.Body.WriteAsync(
            stream.GetBuffer().AsMemory(0, (int)stream.Length),
            context.RequestAborted);
    }
}
