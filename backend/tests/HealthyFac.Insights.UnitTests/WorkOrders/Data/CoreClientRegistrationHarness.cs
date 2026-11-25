using Grpc.Net.ClientFactory;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.Infrastructure.ServiceTokens.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Insights.UnitTests.WorkOrders.Data;

/// <summary>
/// Registers the Core client of Insights with <c>AddCoreClient</c> and the service token, and replaces only the
/// network: the primary handler of the client plays Core, and the one of the token client plays Keycloak.
/// </summary>
public sealed class CoreClientRegistrationHarness : IDisposable
{
    private readonly ServiceProvider _provider;

    public CoreClientRegistrationHarness()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(CoreClientRegistrationTestData.Settings)
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceOptions(configuration);
        services.AddSingleton<ITokenCacheStorage, InMemoryTokenStorage>();
        services.AddSingleton<IIncomingAccessTokenAccessor, NoUserTokenAccessor>();
        services.AddServiceTokens(configuration);
        services.AddCoreClient(configuration);

        services.AddHttpClient(ServiceTokenConstants.TokenClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new FakeTokenEndpointHandler());

        services.AddHttpClient(CoreClientNames.Worker).ConfigurePrimaryHttpMessageHandler(() => Core);
        _provider = services.BuildServiceProvider();
    }

    public CapturingGrpcHandler Core { get; } = new();

    public CoreWorkOrders.CoreWorkOrdersClient CreateClient() =>
        _provider.GetRequiredService<GrpcClientFactory>()
            .CreateClient<CoreWorkOrders.CoreWorkOrdersClient>(CoreClientNames.Worker);

    public void Dispose() => _provider.Dispose();
}
