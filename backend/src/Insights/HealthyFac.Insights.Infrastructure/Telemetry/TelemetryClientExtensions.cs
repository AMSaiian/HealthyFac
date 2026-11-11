using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>
/// Provides extension methods for registering the Telemetry HTTP client.
/// </summary>
public static class TelemetryClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Telemetry client options and the named HTTP client with the service token and the
        /// actor headers. The service must call <c>AddServiceTokens</c>, and the host must register the incoming
        /// token accessor.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddTelemetryClient(IConfiguration configuration)
        {
            services.AddValidatedOptions<TelemetryClientOptions, TelemetryClientOptionsValidator>(
                TelemetryClientOptions.SectionName,
                configuration);

            services.AddHttpClient(
                    TelemetryClientNames.Client,
                    (provider, client) =>
                        client.BaseAddress = new Uri(
                            provider.GetRequiredService<IOptions<TelemetryClientOptions>>().Value.BaseAddress))
                .AddClientCredentialsToken()
                .AddActorHeaders();

            return services;
        }
    }
}
