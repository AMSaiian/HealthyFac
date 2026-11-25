using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Insights.Application.WorkOrders;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.WorkOrders;

/// <summary>
/// Provides extension methods for registering the Core gRPC client.
/// </summary>
public static class CoreClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Core client options, the gRPC work order client with retries, the service token and
        /// the actor headers, and the work order reader. The service must call <c>AddServiceTokens</c>, and the host
        /// must register the incoming token accessor.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddCoreClient(IConfiguration configuration)
        {
            services.AddValidatedOptions<CoreClientOptions, CoreClientOptionsValidator>(
                CoreClientOptions.SectionName,
                configuration);

            services.AddGrpcClient<CoreWorkOrders.CoreWorkOrdersClient>(CoreClientNames.Worker, ConfigureGrpcAddress)
                .ConfigureChannel(options =>
                    options.ServiceConfig = GrpcClientConfigurations.CreateResilientServiceConfig())
                .AddClientCredentialsToken()
                .AddActorHeaders();

            services.AddScoped<IWorkOrderCatalog, CoreWorkOrderCatalog>();

            return services;
        }
    }

    private static void ConfigureGrpcAddress(IServiceProvider provider, GrpcClientFactoryOptions options)
    {
        options.Address = new Uri(provider.GetRequiredService<IOptions<CoreClientOptions>>().Value.GrpcAddress);
    }
}
