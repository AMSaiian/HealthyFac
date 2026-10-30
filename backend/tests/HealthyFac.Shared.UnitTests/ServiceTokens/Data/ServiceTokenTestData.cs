using HealthyFac.Shared.Infrastructure.ServiceTokens;

namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

public static class ServiceTokenTestData
{
    public const string TokenEndpoint = "https://auth.example.test/token";
    public const string ClientId = "healthyfac-test";
    public const string ClientSecret = "test-client-secret";
    public const string ResourceUrl = "https://api.example.test/resource";
    public const string FirstToken = "token-1";
    public const string SecondToken = "token-2";
    public const int TokenLifetimeSeconds = 300;
    public const int RefreshMarginSeconds = 60;
    public const int ParallelCallCount = 8;

    public static readonly TimeSpan ReuseAdvance = TimeSpan.FromSeconds(60);

    public static readonly TimeSpan NearExpiryAdvance =
        TimeSpan.FromSeconds(TokenLifetimeSeconds - RefreshMarginSeconds + 1);

    public static readonly TimeSpan CallsReachCacheDelay = TimeSpan.FromMilliseconds(200);

    public static ServiceTokenOptions Options => CreateOptions();

    public static TheoryData<ServiceTokenOptions> ValidOptions => new()
    {
        CreateOptions(),
        CreateOptions(tokenEndpoint: "http://keycloak:8080/realms/healthyfac/protocol/openid-connect/token"),
    };

    public static TheoryData<ServiceTokenOptions, string> InvalidOptionsWithExpectedError => new()
    {
        {
            CreateOptions(tokenEndpoint: string.Empty),
            ServiceTokenValidationMessages.TokenEndpointInvalid
        },
        {
            CreateOptions(tokenEndpoint: "/relative/token"),
            ServiceTokenValidationMessages.TokenEndpointInvalid
        },
        {
            CreateOptions(tokenEndpoint: "ftp://auth.example.test/token"),
            ServiceTokenValidationMessages.TokenEndpointInvalid
        },
        { CreateOptions(clientId: string.Empty), ServiceTokenValidationMessages.ClientIdRequired },
        { CreateOptions(clientSecret: " "), ServiceTokenValidationMessages.ClientSecretRequired },
    };

    private static ServiceTokenOptions CreateOptions(
        string tokenEndpoint = TokenEndpoint,
        string clientId = ClientId,
        string clientSecret = ClientSecret
    ) =>
        new()
        {
            TokenEndpoint = tokenEndpoint,
            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshMarginSeconds = RefreshMarginSeconds,
        };
}
