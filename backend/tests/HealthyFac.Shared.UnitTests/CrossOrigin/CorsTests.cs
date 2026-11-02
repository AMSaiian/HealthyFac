using System.Net;
using HealthyFac.Shared.UnitTests.CrossOrigin.Data;

namespace HealthyFac.Shared.UnitTests.CrossOrigin;

public sealed class CorsTests
{
    private const string AllowOriginHeader = "Access-Control-Allow-Origin";
    private const string MaxAgeHeader = "Access-Control-Max-Age";

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
    public async Task Preflight_FromOtherOrigin_HasNoAllowOrigin()
    {
        using var host = await CorsHostFactory.StartAsync();

        using var response = await host.SendPreflightAsync(
            CorsHostFactory.EndpointWithPolicy,
            CorsHostFactory.OtherOrigin);

        response.Headers.Contains(AllowOriginHeader).Should().BeFalse();
    }

    [Fact]
    public async Task Preflight_OnEndpointWithoutPolicy_HasNoAllowOrigin()
    {
        using var host = await CorsHostFactory.StartAsync();

        using var response = await host.SendPreflightAsync(
            CorsHostFactory.EndpointWithoutPolicy,
            CorsHostFactory.AllowedOrigin);

        response.Headers.Contains(AllowOriginHeader).Should().BeFalse();
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
