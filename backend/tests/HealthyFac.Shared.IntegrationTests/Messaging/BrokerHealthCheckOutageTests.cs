using System.Diagnostics;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

public sealed class BrokerHealthCheckOutageTests(TransportFixture fixture) : IClassFixture<TransportFixture>
{
    private readonly TransportFixture _fixture = fixture;

    [Fact]
    public async Task BrokerHealthCheck_WhenBrokerIsDown_ReportsDegradedWithinTimeout()
    {
        await using var services = _fixture.BuildBrokerHealthCheckServices();
        var healthCheckService = services.GetRequiredService<HealthCheckService>();
        await _fixture.StopBrokerAsync();
        var stopwatch = Stopwatch.StartNew();

        var report = await healthCheckService.CheckHealthAsync();

        stopwatch.Stop();
        report.Status.Should().Be(HealthStatus.Degraded);
        stopwatch.Elapsed.Should().BeLessThan(BrokerTestData.GatewayProbeTimeout);
    }
}
