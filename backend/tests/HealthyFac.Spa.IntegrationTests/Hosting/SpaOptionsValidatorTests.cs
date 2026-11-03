using HealthyFac.Spa.IntegrationTests.Hosting.Data;
using HealthyFac.Spa.Web.Hosting;

namespace HealthyFac.Spa.IntegrationTests.Hosting;

public sealed class SpaOptionsValidatorTests
{
    private const string ContentRoot = "wwwroot";
    private const string HashPattern = @"-[A-Z0-9]{8}\.";
    private const int CacheSeconds = 31536000;
    private const int RuntimeConfigCacheSeconds = 300;
    private const string PermissionsPolicy = "camera=()";
    private const string AuthRealm = "healthyfac";
    private const string AuthClientId = "healthyfac-spa";

    private readonly SpaOptionsValidator _validator = new();

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.InvalidApiBaseUrls), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithHttpLocalhostOrTrailingSlash_Fails(string apiBaseUrl)
    {
        var options = CreateOptions(apiBaseUrl);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithHttpsApiUrl_Passes()
    {
        var options = CreateOptions();

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.InvalidHashPatterns), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithInvalidHashPattern_Fails(string hashPattern)
    {
        var options = CreateOptions(hashPattern: hashPattern);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.CacheSecondsOutsideRange), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithCacheSecondsOutsideRange_Fails(int cacheSeconds)
    {
        var options = CreateOptions(cacheSeconds: cacheSeconds);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.CacheSecondsAtEdges), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithCacheSecondsAtEdges_Passes(int cacheSeconds)
    {
        var options = CreateOptions(cacheSeconds: cacheSeconds);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(SpaOptionsTestData.RuntimeConfigCacheSecondsOutsideRange), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithRuntimeConfigCacheSecondsOutsideRange_Fails(int cacheSeconds)
    {
        var options = CreateOptions(runtimeConfigCacheSeconds: cacheSeconds);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.RuntimeConfigCacheSecondsAtEdges), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithRuntimeConfigCacheSecondsAtEdges_Passes(int cacheSeconds)
    {
        var options = CreateOptions(runtimeConfigCacheSeconds: cacheSeconds);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithBlankPermissionsPolicy_Fails()
    {
        var options = CreateOptions(permissionsPolicy: " ");

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.InvalidAuthUrls), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithInvalidAuthUrl_Fails(string authUrl)
    {
        var options = CreateOptions(authUrl: authUrl);

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(SpaOptionsTestData.BlankValues), MemberType = typeof(SpaOptionsTestData))]
    public void Validate_WithBlankAuthRealmOrClientId_Fails(string blank)
    {
        var blankRealm = CreateOptions(authRealm: blank);
        var blankClientId = CreateOptions(authClientId: blank);

        var realmResult = _validator.Validate(blankRealm);
        var clientIdResult = _validator.Validate(blankClientId);

        realmResult.IsValid.Should().BeFalse();
        clientIdResult.IsValid.Should().BeFalse();
    }

    private static SpaOptions CreateOptions(
        string apiBaseUrl = SpaFactory.ApiBaseUrl,
        string authUrl = SpaFactory.AuthUrl,
        string authRealm = AuthRealm,
        string authClientId = AuthClientId,
        string hashPattern = HashPattern,
        int cacheSeconds = CacheSeconds,
        string permissionsPolicy = PermissionsPolicy,
        int runtimeConfigCacheSeconds = RuntimeConfigCacheSeconds
    ) => new()
    {
        ContentRoot = ContentRoot,
        ApiBaseUrl = apiBaseUrl,
        AuthUrl = authUrl,
        AuthRealm = authRealm,
        AuthClientId = authClientId,
        HashedFileNamePattern = hashPattern,
        ImmutableCacheSeconds = cacheSeconds,
        PermissionsPolicy = permissionsPolicy,
        RuntimeConfigCacheSeconds = runtimeConfigCacheSeconds,
    };
}
