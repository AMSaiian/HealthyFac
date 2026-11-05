using HealthyFac.Spa.Web.IndexDocument.Csp;
using HealthyFac.Spa.Web.IndexDocument;

namespace HealthyFac.Spa.IntegrationTests.IndexDocument;

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
                + "style-src-attr 'unsafe-inline'; img-src 'self' data:; font-src 'self'; "
                + "connect-src 'self' https://api.healthyfac.test https://auth.healthyfac.test; "
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

        policy.Should().Contain($"connect-src 'self' {ApiBaseUrl} {AuthOrigin};");
        policy.Should().Contain($"frame-src 'self' {AuthOrigin};");
        policy.Should().Contain("frame-ancestors 'none'");
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
