using System.Diagnostics.Metrics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics.Testing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation;

[Collection(TelemetryEvaluationCollection.Name)]
[Feature(TestFeatures.Evaluation)]
public sealed class SilenceTests(TelemetryEvaluationFixture fixture)
{
    private const int SilenceLimitSeconds = 120;

    private readonly TelemetryEvaluationFixture _fixture = fixture;

    [Fact]
    public async Task Evaluate_WithASilentOperationalMachine_OpensAnAlarmAndTheNextMessageResolvesIt()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 11, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 140));
        await _fixture.EvaluateAsync(arrangement.MachineId);
        var opened = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.At(seconds: 150)));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 160));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var resolved = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();
        var messages = await _fixture.LoadEpisodeMessagesAsync(arrangement.MachineId);

        opened.Rule.Should().Be(SignalRule.SilentMachine);
        opened.Severity.Should().Be(SignalSeverity.Alarm);
        opened.State.Should().Be(SignalEpisodeState.Open);
        opened.OpenedAt.Should().Be(_fixture.At(10 + SilenceLimitSeconds));
        resolved.State.Should().Be(SignalEpisodeState.Resolved);
        resolved.ResolvedAt.Should().Be(_fixture.At(seconds: 150));
        messages.Select(message => message.Sequence).Should().Equal(1, 2);
    }

    [Fact]
    public async Task Evaluate_WithAMachineThatNeverSentAMessage_OpensOnlyAfterTheGoLiveStartPlusTheLimit()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.Database.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                arrangement.MachineId,
                MachineRegistrationTestData.GoLiveDay,
                _fixture.At(seconds: 1000),
                _fixture.At(seconds: 0)));

        var counts = new List<int>();

        foreach (var seconds in new[] { 500, 1100, 1120, 1121 })
        {
            _fixture.Time.SetUtcNow(_fixture.At(seconds));
            await _fixture.EvaluateAsync(arrangement.MachineId);
            counts.Add((await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Count);
        }

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        counts.Should().Equal(0, 0, 0, 1);
        episode.OpenedAt.Should().Be(_fixture.At(1000 + SilenceLimitSeconds));
    }

    [Fact]
    public async Task Evaluate_BeforeTheGoLiveStart_OpensNothingEvenWithAnOldLastMessage()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.Database.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                arrangement.MachineId,
                MachineRegistrationTestData.GoLiveDay,
                _fixture.At(seconds: 1000),
                _fixture.At(seconds: 0)));

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 500));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);

        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WithAMessageBeforeTheGoLiveStart_OpensOnlyAtTheGoLiveStartPlusTheLimit()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.Database.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                arrangement.MachineId,
                MachineRegistrationTestData.GoLiveDay,
                _fixture.At(seconds: 1000),
                _fixture.At(seconds: 0)));

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.Origin));

        var counts = new List<int>();

        foreach (var seconds in new[] { 1100, 1120, 1121 })
        {
            _fixture.Time.SetUtcNow(_fixture.At(seconds));
            await _fixture.EvaluateAsync(arrangement.MachineId);
            counts.Add((await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Count);
        }

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        counts.Should().Equal(0, 0, 1);
        episode.OpenedAt.Should().Be(_fixture.At(1000 + SilenceLimitSeconds));
    }

    [Fact]
    public async Task Evaluate_WithAMessageAfterTheGoLiveStart_OpensAtTheLastMessagePlusTheLimit()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.Database.ChangeMachineGoLiveDateAsync(
            MachineRegistrationTestData.CreateGoLiveMessage(
                arrangement.MachineId,
                MachineRegistrationTestData.GoLiveDay,
                _fixture.At(seconds: -100),
                _fixture.At(seconds: -200)));

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.At(seconds: 10)));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 140));
        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episode = (await _fixture.LoadEpisodesAsync(arrangement.MachineId)).Single();

        episode.OpenedAt.Should().Be(_fixture.At(10 + SilenceLimitSeconds));
    }

    [Fact]
    public async Task Evaluate_WhileTheWholeFleetIsSilent_OpensNoSilentMachineEpisode()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 11, _fixture.Origin));

        _fixture.Time.SetUtcNow(_fixture.At(seconds: 300));
        await CheckFleetAsync(_fixture.At(seconds: 300));

        await _fixture.EvaluateAsync(arrangement.MachineId);

        var episodes = await _fixture.LoadEpisodesAsync(arrangement.MachineId);
        var isSilent = _fixture.Database.GetSingleton<FleetSilence>().IsSilent;

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.At(seconds: 300)));

        await CheckFleetAsync(_fixture.At(seconds: 300));

        isSilent.Should().BeTrue();
        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task CheckAsync_WhenTheFleetTurnsSilent_LogsOneWarningAndCountsOnce()
    {
        var arrangement = await _fixture.ArrangeEvaluationMachineAsync();

        await _fixture.SeedSamplesAsync(
            arrangement, EvaluationTestData.Constant(arrangement, value: 50, count: 1, _fixture.Origin));

        var meterFactory = new ServiceCollection().AddMetrics()
            .BuildServiceProvider()
            .GetRequiredService<IMeterFactory>();

        using var collector = new MetricCollector<long>(
            meterFactory,
            ObservabilityConstants.Evaluation.Meter,
            ObservabilityConstants.Evaluation.Instruments.FleetSilences);

        var logger = new FakeLogger<FleetSilence>();

        var fleet = new FleetSilence(
            _fixture.Database.GetSingleton<IOptions<DetectionOptions>>(),
            new DetectionMetrics(meterFactory),
            logger);

        var silentAt = _fixture.At(SilenceLimitSeconds + 1);
        var aliveAt = _fixture.At(SilenceLimitSeconds - 1);
        var results = new List<bool>();

        foreach (var now in new[] { aliveAt, silentAt, silentAt.AddSeconds(value: 10) })
        {
            using var scope = _fixture.Database.ScopeFactory.CreateScope();

            results.Add(
                await fleet.CheckAsync(
                    scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>(), now, CancellationToken.None));
        }

        var warnings = logger.Collector.GetSnapshot().Where(record => record.Level == LogLevel.Warning).ToList();

        results.Should().Equal(false, true, true);
        warnings.Should().ContainSingle().Which.Message.Should().Contain(SilenceLimitSeconds.ToString());
        collector.GetMeasurementSnapshot().Sum(measurement => measurement.Value).Should().Be(expected: 1);
    }

    private async Task CheckFleetAsync(DateTime now)
    {
        using var scope = _fixture.Database.ScopeFactory.CreateScope();

        await _fixture.Database.GetSingleton<FleetSilence>()
            .CheckAsync(
                scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>(), now, CancellationToken.None);
    }
}
