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
    public const int RefreshJitterSeconds = 40;
    public const double FullJitterFraction = 1;
    public const int LockTimeoutSeconds = 10;
    public const int LockPollMilliseconds = 100;
    public const int LockPollJustBelowTimeoutMilliseconds = 9999;
    public const int LockPollEqualToTimeoutMilliseconds = 10000;
    public const string OtherInstanceToken = "token-from-other-instance";
    public const string OtherInstanceOwner = "other-instance";
    public const int ParallelCallCount = 8;
    public const string ServiceName = "auth";
    public const string FirstDownstreamScope = "provisioning";
    public const string SecondDownstreamScope = "reporting";
    public const string FirstPrefixedScope = "auth-provisioning";
    public const string SecondPrefixedScope = "auth-reporting";
    public const string TargetAudience = "healthyfac-test-target";
    public const string TargetScope = "test-target";
    public const string ExpectedExchangeScope = "test-target organization";
    public const string FirstSubjectToken = "subject-token-a";
    public const string SecondSubjectToken = "subject-token-b";
    public const string InvalidSubjectDescription = "Invalid token";
    public const string InvalidClientDescription = "Invalid client or Invalid client credentials";

    public const string InvalidSubjectBody =
        $$"""{"error":"invalid_request","error_description":"{{InvalidSubjectDescription}}"}""";

    public const string InvalidClientBody =
        $$"""{"error":"invalid_client","error_description":"{{InvalidClientDescription}}"}""";

    public static readonly Guid FirstUserId = Guid.Parse("019a0000-0000-7000-8000-0000000000a1");
    public static readonly Guid SecondUserId = Guid.Parse("019a0000-0000-7000-8000-0000000000a2");

    public static readonly TimeSpan ReuseAdvance = TimeSpan.FromSeconds(60);

    public static readonly TimeSpan NearExpiryAdvance =
        TimeSpan.FromSeconds(TokenLifetimeSeconds - RefreshMarginSeconds + 1);

    public static readonly TimeSpan AfterFullJitterAdvance =
        TimeSpan.FromSeconds(TokenLifetimeSeconds - RefreshMarginSeconds - RefreshJitterSeconds + 1);

    public static readonly TimeSpan BeforeFullJitterAdvance =
        TimeSpan.FromSeconds(TokenLifetimeSeconds - RefreshMarginSeconds - RefreshJitterSeconds - 1);

    public static readonly TimeSpan PollAdvance = TimeSpan.FromMilliseconds(LockPollMilliseconds);

    public static readonly TimeSpan AfterLockTimeoutAdvance = TimeSpan.FromSeconds(LockTimeoutSeconds + 1);

    public static readonly TimeSpan CallsReachCacheDelay = TimeSpan.FromMilliseconds(200);

    public static ServiceTokenOptions Options => CreateOptions();

    public static string TokenKey => TokenCacheKeys.ForServiceToken(ServiceName, string.Empty);

    public static string LockKey => TokenCacheKeys.ForLock(ServiceName, TokenKey);

    public static TheoryData<ServiceTokenOptions> ValidOptions => new()
    {
        CreateOptions(),
        CreateOptions("http://keycloak:8080/realms/healthyfac/protocol/openid-connect/token"),
        CreateOptions(lockPollMilliseconds: LockPollJustBelowTimeoutMilliseconds),
    };

    public static TheoryData<ServiceTokenOptions, string> InvalidOptionsWithExpectedError => new()
    {
        {
            CreateOptions(string.Empty),
            ServiceTokenValidationMessages.TokenEndpointInvalid
        },
        {
            CreateOptions("/relative/token"),
            ServiceTokenValidationMessages.TokenEndpointInvalid
        },
        {
            CreateOptions("ftp://auth.example.test/token"),
            ServiceTokenValidationMessages.TokenEndpointInvalid
        },
        { CreateOptions(clientId: string.Empty), ServiceTokenValidationMessages.ClientIdRequired },
        { CreateOptions(clientSecret: " "), ServiceTokenValidationMessages.ClientSecretRequired },
        {
            CreateOptions(lockPollMilliseconds: LockPollEqualToTimeoutMilliseconds),
            ServiceTokenValidationMessages.LockPollNotBelowTimeout
        },
    };

    private static ServiceTokenOptions CreateOptions(
        string tokenEndpoint = TokenEndpoint,
        string clientId = ClientId,
        string clientSecret = ClientSecret,
        int lockPollMilliseconds = LockPollMilliseconds
    ) =>
        new()
        {
            TokenEndpoint = tokenEndpoint,
            ClientId = clientId,
            ClientSecret = clientSecret,
            RefreshMarginSeconds = RefreshMarginSeconds,
            RefreshJitterSeconds = RefreshJitterSeconds,
            LockTimeoutSeconds = LockTimeoutSeconds,
            LockPollMilliseconds = lockPollMilliseconds,
        };
}
