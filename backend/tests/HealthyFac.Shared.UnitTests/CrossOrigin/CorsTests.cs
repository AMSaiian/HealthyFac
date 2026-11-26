using System.Net;
using HealthyFac.Shared.UnitTests.CrossOrigin.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.CrossOrigin;

[Feature(TestFeatures.Platform)]
public sealed class CorsTests
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";
    private const string MaxAgeHeader = "Access-Control-Max-Age";
    private const string ExposeHeadersHeader = "Access-Control-Expose-Headers";

    [Fact]
    public async Task Preflight_FromAllowedOrigin_Returns204WithAllowOrigin()
    {
        using var host = await CorsHostFactory.StartAsync();

        using var response = await host.SendPreflightAsync(
            CorsHostFactory.EndpointWithPolicy,
            CorsHostFactory.AllowedOrigin);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.GetValues(AllowOriginHeader).Should().Equal(CorsHostFactory.AllowedOrigin);
    }

    [Fact]
    public async Task Request_FromAllowedOrigin_ExposesRetryAfterToTheBrowser()
    {
        using var host = await CorsHostFactory.StartAsync();

        using var response = await host.SendCrossOriginPostAsync(
            CorsHostFactory.EndpointWithPolicy,
            CorsHostFactory.AllowedOrigin);

        response.Headers.GetValues(ExposeHeadersHeader).Should().Equal("Retry-After");
    }

    [Theory]
    [MemberData(nameof(CorsTestData.RejectedPreflights), MemberType = typeof(CorsTestData))]
    public async Task Preflight_WithoutAMatchingPolicyOrOrigin_HasNoAllowOrigin(
        string caseName,
        string endpoint,
        string origin
    )
    {
        using var host = await CorsHostFactory.StartAsync();

        using var response = await host.SendPreflightAsync(endpoint, origin);

        response.Headers.Contains(AllowOriginHeader).Should().BeFalse(caseName);
    }

    [Fact]
    public async Task Preflight_FromAllowedOrigin_SetsMaxAge()
    {
        using var host = await CorsHostFactory.StartAsync();

        using var response = await host.SendPreflightAsync(
            CorsHostFactory.EndpointWithPolicy,
            CorsHostFactory.AllowedOrigin);

        response.Headers.GetValues(MaxAgeHeader)
            .Should()
            .Equal(CorsHostFactory.PreflightMaxAgeSeconds.ToString());
    }
}
