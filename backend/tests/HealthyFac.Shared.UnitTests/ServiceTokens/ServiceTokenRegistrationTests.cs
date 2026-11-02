using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Shared.UnitTests.ServiceTokens;

public sealed class ServiceTokenRegistrationTests
{
    [Fact]
    public void AddServiceTokens_WithoutIncomingTokenAccessor_PassesBuildValidation()
    {
        var services = CreateServices();
        services.AddHttpClient(ServiceTokenTestData.ClientId).AddClientCredentialsToken();

        var act = () => services.BuildServiceProvider(ValidatedBuild).Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void AddActorHeaders_WithoutIncomingTokenAccessor_FailsBuildValidation()
    {
        var services = CreateServices();
        services.AddHttpClient(ServiceTokenTestData.ClientId).AddClientCredentialsToken().AddActorHeaders();

        var act = () => services.BuildServiceProvider(ValidatedBuild).Dispose();

        act.Should().Throw<AggregateException>().WithMessage($"*{nameof(ActorHeadersHandler)}*");
    }

    private static ServiceProviderOptions ValidatedBuild => new() { ValidateOnBuild = true };

    private static ServiceCollection CreateServices()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceOptions(configuration);
        services.AddSharedRedis(configuration);
        services.AddServiceTokens(configuration);

        return services;
    }
}
