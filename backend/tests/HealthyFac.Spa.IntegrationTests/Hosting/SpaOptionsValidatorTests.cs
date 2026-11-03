using HealthyFac.Spa.IntegrationTests.Hosting.Data;
using HealthyFac.Spa.Web.Hosting;

namespace HealthyFac.Spa.IntegrationTests.Hosting;

public sealed class SpaOptionsValidatorTests
{
    private const string ContentRoot = "wwwroot";
    private const string HashPattern = @"-[A-Z0-9]{8}\.";
    private const int CacheSeconds = 31536000;
    private const string PermissionsPolicy = "camera=()";

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

    [Fact]
    public void Validate_WithBlankPermissionsPolicy_Fails()
    {
        var options = CreateOptions(permissionsPolicy: " ");

        var result = _validator.Validate(options);

        result.IsValid.Should().BeFalse();
    }

    private static SpaOptions CreateOptions(
        string apiBaseUrl = SpaFactory.ApiBaseUrl,
        string hashPattern = HashPattern,
        int cacheSeconds = CacheSeconds,
        string permissionsPolicy = PermissionsPolicy
    ) => new()
    {
        ContentRoot = ContentRoot,
        ApiBaseUrl = apiBaseUrl,
        HashedFileNamePattern = hashPattern,
        ImmutableCacheSeconds = cacheSeconds,
        PermissionsPolicy = permissionsPolicy,
    };
}
