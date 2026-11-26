using System.Text.Json;
using HealthyFac.Shared.UnitTests.HealthChecks.Data;
using HealthyFac.Shared.Web.HealthChecks;
using HealthyFac.Testing.Features;
using Microsoft.AspNetCore.Http;

namespace HealthyFac.Shared.UnitTests.HealthChecks;

[Feature(TestFeatures.Platform)]
public sealed class HealthEndpointTests
{
    [Fact]
    public async Task Readiness_OnHealthPort_WithFallbackPolicy_ReturnsStatus()
    {
        using var host = await HealthHostFactory.StartAsync(hasFallbackPolicy: true);

        var response = await host.GetOnPortAsync(HealthCheckConstants.Routes.Readiness, HealthHostFactory.HealthPort);

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        response.Body.Should().Be(nameof(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy));
    }

    [Fact]
    public async Task Liveness_OnHealthPort_ReturnsOk()
    {
        using var host = await HealthHostFactory.StartAsync(hasFallbackPolicy: false);

        var response = await host.GetOnPortAsync(HealthCheckConstants.Routes.Liveness, HealthHostFactory.HealthPort);

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Theory]
    [MemberData(nameof(HealthEndpointTestData.PublicPortRoutes), MemberType = typeof(HealthEndpointTestData))]
    public async Task Health_OnPublicPort_ReturnsNotFound(string caseName, string route)
    {
        using var host = await HealthHostFactory.StartAsync(hasFallbackPolicy: false);

        var response = await host.GetOnPortAsync(route, HealthHostFactory.PublicPort);

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound, caseName);
    }

    [Fact]
    public async Task DetailedHealth_OnHealthPort_ReturnsChecks()
    {
        using var host = await HealthHostFactory.StartAsync(hasFallbackPolicy: false);

        var response = await host.GetOnPortAsync(HealthCheckConstants.Routes.Detailed, HealthHostFactory.HealthPort);

        using var report = JsonDocument.Parse(response.Body);

        var checkNames = report.RootElement
            .GetProperty(HealthCheckConstants.Properties.Checks)
            .EnumerateArray()
            .Select(check => check.GetProperty(HealthCheckConstants.Properties.Name).GetString());

        response.StatusCode.Should().Be(StatusCodes.Status200OK);
        checkNames.Should().Contain(HealthHostFactory.ReadyCheckName);
    }

    [Fact]
    public async Task Readiness_WithHealthPortInHostHeader_ReturnsNotFound()
    {
        using var host = await HealthHostFactory.StartAsync(hasFallbackPolicy: false);

        var response = await host.GetOnPortAsync(
            HealthCheckConstants.Routes.Readiness,
            HealthHostFactory.PublicPort,
            $"localhost:{HealthHostFactory.HealthPort}");

        response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }
}
