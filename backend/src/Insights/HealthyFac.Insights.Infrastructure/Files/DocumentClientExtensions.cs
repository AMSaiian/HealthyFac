using Grpc.Net.ClientFactory;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Insights.Application.Files;
using HealthyFac.Shared.Infrastructure.Configuration;
using HealthyFac.Shared.Infrastructure.Rpc;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Files;

/// <summary>
/// Provides extension methods for registering the Document gRPC clients.
/// </summary>
public static class DocumentClientExtensions
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers the validated Document client options, the user client and the worker client with retries, and
        /// the file catalog. The service must call <c>AddServiceTokens</c>, and the host must register the incoming
        /// token accessor.
        /// </summary>
        /// <param name="configuration">The application <see cref="IConfiguration"/> instance.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance for method chaining.</returns>
        public IServiceCollection AddDocumentClients(IConfiguration configuration)
        {
            services.AddValidatedOptions<DocumentClientOptions, DocumentClientOptionsValidator>(
                DocumentClientOptions.SectionName,
                configuration);

            services.AddGrpcClient<DocumentFiles.DocumentFilesClient>(
                    DocumentClientNames.User,
                    ConfigureAddress)
                .ConfigureChannel(options =>
                    options.ServiceConfig = GrpcClientConfigurations.CreateResilientServiceConfig())
                .AddUserDelegatedToken(DocumentClientNames.Audience, DocumentClientNames.UserTokenScope);

            services.AddGrpcClient<DocumentFiles.DocumentFilesClient>(
                    DocumentClientNames.Worker,
                    ConfigureAddress)
                .ConfigureChannel(options =>
                    options.ServiceConfig = GrpcClientConfigurations.CreateResilientServiceConfig())
                .AddClientCredentialsToken()
                .AddActorHeaders();

            services.AddScoped<IFileCatalog, DocumentGrpcFileCatalog>();

            return services;
        }
    }

    private static void ConfigureAddress(IServiceProvider provider, GrpcClientFactoryOptions options)
    {
        options.Address = new Uri(provider.GetRequiredService<IOptions<DocumentClientOptions>>().Value.GrpcAddress);
    }
}
