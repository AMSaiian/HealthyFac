using HealthyFac.Spa.IntegrationTests.IndexDocument.Data;
using HealthyFac.Spa.Web.IndexDocument.Csp;
using HealthyFac.Testing.Features;

namespace HealthyFac.Spa.IntegrationTests.IndexDocument;

[Feature(TestFeatures.Platform)]
public sealed class IndexDocumentTests
{
    private const string ApiBaseUrl = "https://api.healthyfac.test";
    private const string AuthOrigin = "https://auth.healthyfac.test";
    private const string Nonce = "abc123";

    [Fact]
    public void Build_WithNonceAndUrls_ContainsNonceAndBothOrigins()
    {
        var policy = ContentSecurityPolicy.Build(Nonce, new Uri($"{ApiBaseUrl}/"), new Uri($"{AuthOrigin}/"));

        policy.Should()
            .Be(
                "default-src 'self'; script-src 'self' 'nonce-abc123'; style-src-elem 'self' 'nonce-abc123'; "
                + "style-src-attr 'unsafe-inline'; img-src 'self' data: https://api.healthyfac.test; "
                + "font-src 'self'; "
                + "connect-src 'self' https://api.healthyfac.test wss://api.healthyfac.test "
                + "https://auth.healthyfac.test; "
                + "frame-src 'self' https://auth.healthyfac.test; base-uri 'self'; form-action 'self'; "
                + "frame-ancestors 'none'; object-src 'none'");
    }

    [Fact]
    public void Build_WithAuthUrl_AllowsAuthOriginInConnectAndFrameSrc()
    {
        var policy = ContentSecurityPolicy.Build(
            Nonce,
            new Uri(ApiBaseUrl),
            new Uri($"{AuthOrigin}/realms"));

        policy.Should().Contain($"connect-src 'self' {ApiBaseUrl} wss://api.healthyfac.test {AuthOrigin};");
        policy.Should().Contain($"frame-src 'self' {AuthOrigin};");
        policy.Should().Contain(expected: "frame-ancestors 'none'");
    }

    [Theory]
    [MemberData(nameof(IndexDocumentTestData.WebSocketOrigins), MemberType = typeof(IndexDocumentTestData))]
    public void Build_WithApiUrl_AllowsItsWebSocketOriginInConnectSrc(
        string apiBaseUrl,
        string expectedConnectSrc
    )
    {
        var policy = ContentSecurityPolicy.Build(Nonce, new Uri(apiBaseUrl), new Uri(AuthOrigin));

        policy.Should().Contain(expectedConnectSrc);
    }

    [Fact]
    public void Build_WithApiUrl_AllowsTheApiOriginForImagesAndKeepsSelfAndData()
    {
        var policy = ContentSecurityPolicy.Build(Nonce, new Uri($"{ApiBaseUrl}/v1/"), new Uri(AuthOrigin));

        policy.Should().Contain($"img-src 'self' data: {ApiBaseUrl};");
    }

    [Fact]
    public void Create_CalledTwice_ReturnsDifferentValues()
    {
        var first = CspNonce.Create();
        var second = CspNonce.Create();

        first.Should().NotBe(second);
        first.Should().NotBeNullOrEmpty();
    }
}
