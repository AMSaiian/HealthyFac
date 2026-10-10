using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Shared.Web.HealthChecks;

/// <summary>
/// Provides extension methods for registering and exposing health check endpoints.
/// </summary>
public static class HealthCheckExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the health check service so each microservice can append its own checks.
        /// Tag a check with <see cref="HealthCheckConstants.ReadyTag"/> to include it in readiness.
        /// </summary>
        /// <returns>The <see cref="IHealthChecksBuilder"/> for registering individual checks.</returns>
        public IHealthChecksBuilder AddSharedHealthChecks()
            => services.AddHealthChecks();
    }

    extension(IEndpointRouteBuilder app)
    {
        /// <summary>
        /// Maps three health endpoints: a liveness pull hook that runs no checks and always answers 200,
        /// a readiness endpoint limited to checks tagged ready, and a detailed endpoint reporting every check.
        /// </summary>
        /// <returns>The <see cref="IEndpointRouteBuilder"/> instance for chained configuration.</returns>
        public IEndpointRouteBuilder MapSharedHealthChecks()
        {
            app.MapHealthChecks(
                HealthCheckConstants.Routes.Liveness,
                new HealthCheckOptions
                {
                    Predicate = _ => false,
                    ResponseWriter = WriteLivenessAsync,
                });

            app.MapHealthChecks(
                HealthCheckConstants.Routes.Readiness,
                new HealthCheckOptions
                {
                    Predicate = registration => registration.Tags.Contains(HealthCheckConstants.ReadyTag),
                    ResponseWriter = WriteLivenessAsync,
                });

            app.MapHealthChecks(
                HealthCheckConstants.Routes.Detailed,
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
        context.Response.ContentType = HealthCheckConstants.ContentTypes.PlainText;

        return context.Response.WriteAsync(
            report.Status == HealthStatus.Healthy
                ? HealthCheckConstants.HealthyResponse
                : report.Status.ToString(),
            context.RequestAborted);
    }

    private static async Task WriteDetailedAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = HealthCheckConstants.ContentTypes.Json;

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
