using System.Diagnostics;
using HealthyFac.Shared.IntegrationTests.Messaging.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HealthyFac.Shared.IntegrationTests.Messaging;

[Collection(MessagingBrokerOutageCollection.Name)]
public sealed class BrokerHealthCheckOutageTests(TransportCollectionFixture fixture)
{
    private readonly TransportCollectionFixture _fixture = fixture;

    [Fact]
    public async Task BrokerHealthCheck_WhenBrokerIsDown_ReportsDegradedWithinTimeout()
    {
        await using var services = _fixture.BuildBrokerHealthCheckServices();
        var healthCheckService = services.GetRequiredService<HealthCheckService>();
        await _fixture.StopBrokerAsync();
        var stopwatch = Stopwatch.StartNew();

        var report = await healthCheckService.CheckHealthAsync(TestContext.Current.CancellationToken);

        stopwatch.Stop();
        report.Status.Should().Be(HealthStatus.Degraded);
        stopwatch.Elapsed.Should().BeLessThan(BrokerTestData.GatewayProbeTimeout);
    }
}
