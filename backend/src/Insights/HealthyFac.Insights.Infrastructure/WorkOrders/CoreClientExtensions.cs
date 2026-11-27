using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.Companies;
using HealthyFac.Core.Contracts.Employees;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Core.Contracts.Schedules;
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
        /// the actor headers, the user clients (machines, work orders, employees, schedules, and companies, with the
        /// exchanged token of the user and no actor headers), and the work order reader. The service must call
        /// <c>AddServiceTokens</c>, and the host must register the incoming token accessor.
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

            AddUserClient<CoreMachines.CoreMachinesClient>(services, CoreClientNames.User.Machines);
            AddUserClient<CoreWorkOrders.CoreWorkOrdersClient>(services, CoreClientNames.User.WorkOrders);
            AddUserClient<CoreEmployees.CoreEmployeesClient>(services, CoreClientNames.User.Employees);
            AddUserClient<CoreSchedules.CoreSchedulesClient>(services, CoreClientNames.User.Schedules);
            AddUserClient<CoreCompanies.CoreCompaniesClient>(services, CoreClientNames.User.Companies);

            services.AddScoped<IWorkOrderCatalog, CoreWorkOrderCatalog>();

            return services;
        }
    }

    private static void AddUserClient<TClient>(IServiceCollection services, string name)
        where TClient : class =>
        services.AddGrpcClient<TClient>(name, ConfigureGrpcAddress)
            .ConfigureChannel(options =>
                options.ServiceConfig = GrpcClientConfigurations.CreateResilientServiceConfig())
            .AddUserDelegatedToken(CoreClientNames.UserToken.Audience, CoreClientNames.UserToken.Scope);

    private static void ConfigureGrpcAddress(IServiceProvider provider, GrpcClientFactoryOptions options)
    {
        options.Address = new Uri(provider.GetRequiredService<IOptions<CoreClientOptions>>().Value.GrpcAddress);
    }
}
