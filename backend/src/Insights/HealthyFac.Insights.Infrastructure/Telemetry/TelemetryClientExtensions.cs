using Grpc.Net.ClientFactory;
using HealthyFac.Insights.Application.Signals;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Telemetry.Contracts.Signals;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>
/// Provides extension methods for registering the Telemetry HTTP client and the Telemetry gRPC client.
/// </summary>
public static class TelemetryClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Telemetry client options, the named HTTP client, and the gRPC signals client with
        /// retries, both with the service token and the actor headers, and the signal reader. The service must call
        /// <c>AddServiceTokens</c>, and the host must register the incoming token accessor.
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

            services.AddGrpcClient<TelemetrySignals.TelemetrySignalsClient>(
                    TelemetryClientNames.Signals,
                    ConfigureGrpcAddress)
                .ConfigureChannel(options =>
                    options.ServiceConfig = GrpcClientConfigurations.CreateResilientServiceConfig())
                .AddClientCredentialsToken()
                .AddActorHeaders();

            services.AddScoped<ITelemetrySignals, GrpcTelemetrySignals>();

            return services;
        }
    }

    private static void ConfigureGrpcAddress(IServiceProvider provider, GrpcClientFactoryOptions options)
    {
        options.Address = new Uri(provider.GetRequiredService<IOptions<TelemetryClientOptions>>().Value.GrpcAddress);
    }
}
