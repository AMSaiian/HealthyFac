using System.Diagnostics.Metrics;
using System.Net;
using System.Text;
using HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Testing.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation;

[Collection(SimulatorOutageCollection.Name)]
[Feature(TestFeatures.Simulation)]
public sealed class HostHealthTests(SimulatorFixture fixture)
{
    private readonly SimulatorFixture _fixture = fixture;

    [Fact]
    public async Task HealthReady_WithBrokerRunning_IsHealthy()
    {
        var response = await ReadReadinessAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Body.Should().Be(SimulatorTestData.HealthyBody);
    }

    [Fact]
    public async Task HealthReady_WithBrokerStoppedThenStarted_IsUnhealthyThenHealthy()
    {
        await _fixture.StopBrokerAsync();
        await WaitForDisconnectAsync();
        var whileStopped = await ReadReadinessAsync();
        await _fixture.RestartBrokerAsync();
        await _fixture.WaitForConnectionAsync();
        var afterStart = await ReadReadinessAsync();

        whileStopped.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        whileStopped.Body.Should().Be(SimulatorTestData.UnhealthyBody);
        afterStart.Body.Should().Be(SimulatorTestData.HealthyBody);
    }

    [Fact]
    public async Task Tick_WithBrokerStopped_DropsMessagesLogsOneWarningAndResumesAfterTheRestart()
    {
        await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);

        using var collector = new MetricCollector<long>(
            _fixture.GetSimulatorSingleton<IMeterFactory>(),
            ObservabilityConstants.Simulator.Meter,
            ObservabilityConstants.Simulator.Instruments.Messages);

        await _fixture.StopBrokerAsync();
        await WaitForDisconnectAsync();
        await AdvanceUntilAsync(() => CountOutcome(collector, SimulatorTestData.DroppedOutcome) >= 2);
        await _fixture.RestartBrokerAsync();
        await _fixture.WaitForConnectionAsync();

        var afterRestart = await _fixture.TickAsync();

        var warnings = _fixture.Logs.Entries
            .Where(entry => entry.Level == LogLevel.Warning
                && entry.Message.Contains(SimulatorTestData.DisconnectWarning))
            .ToList();

        warnings.Should().ContainSingle();
        CountOutcome(collector, SimulatorTestData.DroppedOutcome).Should().BeGreaterThanOrEqualTo(expected: 2);
        afterRestart.Should().ContainSingle();
    }

    [Fact]
    public async Task Discover_WithNewMachine_LogsInformationWithoutAnySecret()
    {
        await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);

        var entries = _fixture.Logs.Entries;

        entries
            .Count(entry => entry.Level == LogLevel.Information
                && entry.Message.StartsWith(SimulatorTestData.FoundMachineInformation))
            .Should()
            .Be(expected: 1);

        entries
            .Where(entry => entry.Message.Contains(_fixture.Broker.SenderPassword)
                || entry.Message.Contains(SimulatorTestData.PasswordMarker)
                || (entry.Exception?.Message.Contains(SimulatorTestData.PasswordMarker) ?? false))
            .Should()
            .BeEmpty();
    }

    private static int CountOutcome(MetricCollector<long> collector, string outcome) =>
        collector.GetMeasurementSnapshot()
            .Count(measurement => (string?)measurement.Tags[ObservabilityConstants.Simulator.Tags.Outcome] == outcome);

    private async Task AdvanceUntilAsync(Func<bool> isDone)
    {
        var deadline = DateTime.UtcNow + SimulatorTestData.WaitLimit;

        while (!isDone() && DateTime.UtcNow < deadline)
        {
            _fixture.Clock.Advance(SimulatorTestData.ReconnectDelay);
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 50));
        }

        isDone().Should().BeTrue();
    }

    private async Task WaitForDisconnectAsync()
    {
        var deadline = DateTime.UtcNow + SimulatorTestData.WaitLimit;

        while (_fixture.Publisher.IsConnected && DateTime.UtcNow < deadline)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 100));
        }

        _fixture.Publisher.IsConnected.Should().BeFalse();
    }

    private async Task<HealthResponse> ReadReadinessAsync()
    {
        var context = await _fixture.Factory.Server.SendAsync(httpContext =>
        {
            httpContext.Request.Method = HttpMethods.Get;
            httpContext.Request.Path = SimulatorTestData.HealthReadyRoute;
            httpContext.Connection.LocalPort = SimulatorTestData.HealthPort;
        });

        using var reader = new StreamReader(context.Response.Body, Encoding.UTF8);

        return new HealthResponse((HttpStatusCode)context.Response.StatusCode, await reader.ReadToEndAsync());
    }

    private sealed record HealthResponse(HttpStatusCode StatusCode, string Body);
}
