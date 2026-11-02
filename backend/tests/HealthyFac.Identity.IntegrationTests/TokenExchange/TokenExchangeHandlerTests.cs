using HealthyFac.Identity.IntegrationTests.Realm.Data;
using HealthyFac.Identity.IntegrationTests.TokenExchange.Data;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.Caching;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Identity.IntegrationTests.TokenExchange;

public sealed class TokenExchangeHandlerTests(TokenExchangeFixture fixture) : IClassFixture<TokenExchangeFixture>
{
    private readonly TokenExchangeFixture _fixture = fixture;

    [Fact]
    public async Task UserDelegatedHandler_AgainstKeycloak_SendsTokenWithTargetAudience()
    {
        var subjectToken = await _fixture.Realm.GetSupervisorAccessTokenAsync();
        var echo = new EchoHandler();
        using var provider = BuildProvider(subjectToken, echo);
        var clientFactory = provider.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient(TokenExchangeTestData.EchoClientName);

        using var response = await client.GetAsync(TokenExchangeTestData.EchoUrl);

        var claims = KeycloakRealmFixtureExtensions.DecodePayload(echo.ReceivedToken!);
        claims.Audiences().Should().Equal(TokenExchangeTestData.TargetClientId);
        claims[TokenExchangeTestData.SubjectClaim]!.GetValue<string>()
            .Should()
            .Be(RealmTestData.SupervisorIdentityId.ToString());
    }

    private ServiceProvider BuildProvider(string subjectToken, EchoHandler echo)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{ServiceTokenOptions.SectionName}:TokenEndpoint"] =
                        new Uri(new Uri(_fixture.Realm.Server.BaseAddress), TokenExchangeTestData.TokenPath)
                            .ToString(),
                    [$"{ServiceTokenOptions.SectionName}:ClientId"] = RealmTestData.CoreClientId,
                    [$"{ServiceTokenOptions.SectionName}:ClientSecret"] = RealmTestData.CoreClientSecret,
                    [$"{RedisOptions.SectionName}:ConnectionString"] = _fixture.Redis.ConnectionString,
                    [$"{ServiceOptions.SectionName}:Name"] = ServiceName.Core.Value,
                })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddServiceOptions(configuration);
        services.AddSharedRedis(configuration);
        services.AddServiceTokens(configuration);
        services.AddSingleton<IIncomingAccessTokenAccessor>(
            new FixedIncomingAccessTokenAccessor(subjectToken, RealmTestData.SupervisorIdentityId));
        services.AddHttpClient(TokenExchangeTestData.EchoClientName)
            .AddUserDelegatedToken(TokenExchangeTestData.TargetClientId, TokenExchangeTestData.ExchangeScopes)
            .ConfigurePrimaryHttpMessageHandler(() => echo);

        return services.BuildServiceProvider();
    }
}
