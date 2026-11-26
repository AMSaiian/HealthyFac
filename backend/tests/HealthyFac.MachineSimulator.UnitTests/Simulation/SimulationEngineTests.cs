using System.Diagnostics.Metrics;
using System.Text.Json;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.MachineSimulator.Scenarios.Modules;
using HealthyFac.MachineSimulator.Simulation;
using HealthyFac.MachineSimulator.UnitTests.Simulation.Data;
using HealthyFac.Shared.Application.Observability;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.MachineSimulator.UnitTests.Simulation;

public sealed class SimulationEngineTests
{
    private readonly FakeTimeProvider _time = new(SimulationEngineTestData.Start);
    private readonly FakeMachineMessagePublisher _publisher = new();
    private readonly SimulationEngine _engine;
    private readonly SimulationLoops _loops;

    private readonly IMeterFactory _meterFactory = new ServiceCollection()
        .AddMetrics()
        .BuildServiceProvider()
        .GetRequiredService<IMeterFactory>();

    public SimulationEngineTests()
    {
        var options = Options.Create(new SimulatorOptions());

        _engine = new SimulationEngine(
            options,
            new Random(SimulationEngineTestData.Seed),
            NullLogger<SimulationEngine>.Instance);

        _loops = new SimulationLoops(
            _engine,
            _publisher,
            new SimulatorMetrics(_meterFactory),
            options,
            new ServiceCollection().BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            _time,
            NullLogger<SimulationLoops>.Instance);
    }

    [Fact]
    public async Task PublishTickAsync_ForThreeSeconds_PublishesThreeMessagesWithTheTickTimes()
    {
        _engine.ApplyDiscovery(
            [SimulationEngineTestData.Snapshot(serialNumber: "GEN-1", SimulationEngineTestData.LevelDefinitions)],
            SimulationEngineTestData.NoCounterStarts,
            _time.GetUtcNow().UtcDateTime);

        var tickTimes = new List<string>();

        for (var second = 0; second < 3; second++)
        {
            _time.Advance(TimeSpan.FromSeconds(seconds: 1));
            tickTimes.Add(_time.GetUtcNow().UtcDateTime.ToString(format: "O"));
            await _loops.PublishTickAsync(CancellationToken.None);
        }

        var sampledAt = _publisher.Messages.Select(message => SampledAtOf(message.Payload)).ToList();
        sampledAt.Should().Equal(tickTimes);
    }

    [Fact]
    public async Task PublishTickAsync_AfterTheStatePollDropsTheMachine_PublishesNothing()
    {
        _engine.ApplyDiscovery(
            [SimulationEngineTestData.Snapshot(serialNumber: "GEN-1", SimulationEngineTestData.LevelDefinitions)],
            SimulationEngineTestData.NoCounterStarts,
            _time.GetUtcNow().UtcDateTime);

        _engine.ApplyStatePoll([], _time.GetUtcNow().UtcDateTime);
        await _loops.PublishTickAsync(CancellationToken.None);

        _publisher.Messages.Should().BeEmpty();
        _engine.KnownMachineIds().Should().BeEmpty();
    }

    [Fact]
    public async Task PublishTickAsync_AfterTheStatePollShowsNoRegistration_PublishesNothing()
    {
        var snapshot = SimulationEngineTestData.Snapshot(
            serialNumber: "GEN-1", SimulationEngineTestData.LevelDefinitions);

        _engine.ApplyDiscovery([snapshot], SimulationEngineTestData.NoCounterStarts, _time.GetUtcNow().UtcDateTime);

        _engine.ApplyStatePoll([snapshot with { IsRegistered = false }], _time.GetUtcNow().UtcDateTime);
        await _loops.PublishTickAsync(CancellationToken.None);

        _publisher.Messages.Should().BeEmpty();
    }

    [Fact]
    public async Task PublishTickAsync_AfterTheStatePollAddsADefinition_IncludesTheKey()
    {
        var snapshot = SimulationEngineTestData.Snapshot(
            serialNumber: "GEN-1", SimulationEngineTestData.LevelDefinitions);

        _engine.ApplyDiscovery([snapshot], SimulationEngineTestData.NoCounterStarts, _time.GetUtcNow().UtcDateTime);
        await _loops.PublishTickAsync(CancellationToken.None);

        _engine.ApplyStatePoll(
            [snapshot with { Definitions = SimulationEngineTestData.LevelAndPressureDefinitions }],
            _time.GetUtcNow().UtcDateTime);

        await _loops.PublishTickAsync(CancellationToken.None);

        MetricKeysOf(_publisher.Messages[0].Payload).Should().Equal("level");
        MetricKeysOf(_publisher.Messages[1].Payload).Should().BeEquivalentTo("level", "pressure");
    }

    [Fact]
    public async Task PublishTickAsync_WithTheBrokerDown_DropsTheMessageAndKeepsTheCountersRunning()
    {
        var snapshot = SimulationEngineTestData.Snapshot(
            ScenarioBinding.CncSerialNumber,
            SimulationEngineTestData.CncDefinitions);

        _engine.ApplyDiscovery([snapshot], SimulationEngineTestData.NoCounterStarts, _time.GetUtcNow().UtcDateTime);
        _publisher.IsAccepting = false;

        using var collector = new MetricCollector<long>(
            _meterFactory,
            ObservabilityConstants.Simulator.Meter,
            ObservabilityConstants.Simulator.Instruments.Messages);

        _time.Advance(TimeSpan.FromSeconds(seconds: 1));
        await _loops.PublishTickAsync(CancellationToken.None);

        var view = _engine.Find(snapshot.MachineId, _time.GetUtcNow().UtcDateTime)!;
        view.Counters["machine.operating_hours"].Should().BeGreaterThan(expected: 0);

        collector.GetMeasurementSnapshot()
            .Should()
            .ContainSingle()
            .Which.Tags[ObservabilityConstants.Simulator.Tags.Outcome]
            .Should()
            .Be(expected: "dropped");
    }

    [Fact]
    public async Task PublishTickAsync_AfterSetModeFailure_SendsTheFailureCodesAtTheNextTick()
    {
        var snapshot = SimulationEngineTestData.Snapshot(
            ScenarioBinding.CncSerialNumber,
            SimulationEngineTestData.CncDefinitions);

        _engine.ApplyDiscovery([snapshot], SimulationEngineTestData.NoCounterStarts, _time.GetUtcNow().UtcDateTime);

        var result = _engine.SetMode(snapshot.MachineId, ModuleName.Spindle, ModuleMode.Failure);
        _time.Advance(TimeSpan.FromSeconds(seconds: 1));
        await _loops.PublishTickAsync(CancellationToken.None);

        result.Should().Be(ControlResult.Done);
        StatusesOf(_publisher.Messages.Single().Payload).Should().BeEquivalentTo("A101", "A111");
    }

    [Fact]
    public void SetMode_WithUnknownMachine_ReportsMachineNotFound()
    {
        var result = _engine.SetMode(Guid.NewGuid(), ModuleName.Spindle, ModuleMode.Failure);

        result.Should().Be(ControlResult.MachineNotFound);
    }

    [Fact]
    public void SetMode_WithModuleThatTheScenarioLacks_ReportsModuleNotFound()
    {
        var snapshot = SimulationEngineTestData.Snapshot(
            serialNumber: "GEN-1", SimulationEngineTestData.LevelDefinitions);

        _engine.ApplyDiscovery([snapshot], SimulationEngineTestData.NoCounterStarts, _time.GetUtcNow().UtcDateTime);

        var result = _engine.SetMode(snapshot.MachineId, ModuleName.Spindle, ModuleMode.Failure);

        result.Should().Be(ControlResult.ModuleNotFound);
    }

    [Fact]
    public async Task StartAsync_WithOneTickThatThrows_PublishesAtTheNextTick()
    {
        var snapshot = SimulationEngineTestData.Snapshot(
            serialNumber: "GEN-1", SimulationEngineTestData.LevelDefinitions);

        _engine.ApplyDiscovery([snapshot], SimulationEngineTestData.NoCounterStarts, _time.GetUtcNow().UtcDateTime);
        _publisher.FailuresLeft = 1;
        await _loops.StartAsync(CancellationToken.None);

        await AdvanceUntilAsync(() => _publisher.Messages.Count > 0);
        await _loops.StopAsync(CancellationToken.None);

        _publisher.Messages.Should().NotBeEmpty();
    }

    private async Task AdvanceUntilAsync(Func<bool> isDone)
    {
        var deadline = DateTime.UtcNow.AddSeconds(value: 10);

        while (!isDone() && DateTime.UtcNow < deadline)
        {
            _time.Advance(TimeSpan.FromSeconds(seconds: 1));
            await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 20));
        }
    }

    private static string SampledAtOf(byte[] payload)
    {
        using var document = JsonDocument.Parse(payload);

        return DateTime.Parse(
                document.RootElement.GetProperty(propertyName: "sampledAt").GetString()!,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.RoundtripKind)
            .ToString(format: "O");
    }

    private static IReadOnlyList<string> MetricKeysOf(byte[] payload)
    {
        using var document = JsonDocument.Parse(payload);

        return
        [
            .. document.RootElement.GetProperty(propertyName: "metrics")
                .EnumerateObject()
                .Select(item => item.Name),
        ];
    }

    private static IReadOnlyList<string> StatusesOf(byte[] payload)
    {
        using var document = JsonDocument.Parse(payload);

        return
        [
            .. document.RootElement.GetProperty(propertyName: "statuses")
                .EnumerateArray()
                .Select(item => item.GetString()!),
        ];
    }
}
