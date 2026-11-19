using Grpc.Core;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Telemetry;
using HealthyFac.Insights.UnitTests.Files.Data;
using HealthyFac.Insights.UnitTests.Signals.Data;
using HealthyFac.Shared.Application.DownstreamScopes;

namespace HealthyFac.Insights.UnitTests.Signals;

public sealed class GrpcTelemetrySignalsTests
{
    private readonly DownstreamScopeHolder _holder = new();
    private readonly FakeDocumentCallInvoker _invoker;
    private readonly GrpcTelemetrySignals _signals;

    public GrpcTelemetrySignalsTests()
    {
        _invoker = new FakeDocumentCallInvoker(_holder);
        _signals = new GrpcTelemetrySignals(new FakeTelemetryClientFactory(_invoker), _holder);
    }

    [Fact]
    public async Task GetMetricHistory_WithBuckets_MapsEveryField()
    {
        _invoker.Respond = _ => SignalsTestData.CreateHistoryReply();

        var buckets = await _signals.GetMetricHistoryAsync(
            SignalsTestData.MachineId,
            SignalsTestData.MetricId,
            SignalsTestData.From,
            SignalsTestData.To,
            bucketCount: 120,
            TestContext.Current.CancellationToken);

        var bucket = buckets.Should().ContainSingle().Subject;
        bucket.Start.Should().Be(SignalsTestData.From);
        bucket.Avg.Should().Be(expected: 15);
        bucket.Min.Should().Be(expected: 10);
        bucket.Max.Should().Be(expected: 20);
        bucket.Count.Should().Be(expected: 2);
    }

    [Fact]
    public async Task GetSignalEpisodes_WithEveryOptionalValue_MapsThem()
    {
        _invoker.Respond = _ => SignalsTestData.CreateEpisodesReply(hasOptionalValues: true);

        var episodes = await _signals.GetSignalEpisodesAsync(
            SignalsTestData.MachineId,
            SignalsTestData.From,
            SignalsTestData.To,
            TestContext.Current.CancellationToken);

        var episode = episodes.Should().ContainSingle().Subject;
        episode.EpisodeId.Should().Be(SignalsTestData.EpisodeId);
        episode.Rule.Should().Be(expected: "threshold");
        episode.MetricDefinitionId.Should().Be(SignalsTestData.MetricId);
        episode.MetricKey.Should().Be(expected: "spindle.temperature");
        episode.PhaseCode.Should().Be(expected: "cutting");
        episode.Value.Should().Be(expected: 91);
        episode.Limit.Should().Be(expected: 90);
        episode.ResolvedAt.Should().Be(SignalsTestData.From.AddMinutes(value: 9));
    }

    [Fact]
    public async Task GetSignalEpisodes_WithoutOptionalValues_MapsThemToNull()
    {
        _invoker.Respond = _ => SignalsTestData.CreateEpisodesReply(hasOptionalValues: false);

        var episodes = await _signals.GetSignalEpisodesAsync(
            SignalsTestData.MachineId,
            SignalsTestData.From,
            SignalsTestData.To,
            TestContext.Current.CancellationToken);

        var episode = episodes.Should().ContainSingle().Subject;
        episode.MetricDefinitionId.Should().BeNull();
        episode.MetricKey.Should().BeNull();
        episode.Value.Should().BeNull();
        episode.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetMetricHistory_WhenTelemetryAnswersNotFound_ReturnsAnEmptyList()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "missing"));

        var buckets = await _signals.GetMetricHistoryAsync(
            SignalsTestData.MachineId,
            SignalsTestData.MetricId,
            SignalsTestData.From,
            SignalsTestData.To,
            bucketCount: 120,
            TestContext.Current.CancellationToken);

        buckets.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSignalEpisodes_WhenTelemetryAnswersNotFound_ReturnsAnEmptyList()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "missing"));

        var episodes = await _signals.GetSignalEpisodesAsync(
            SignalsTestData.MachineId,
            SignalsTestData.From,
            SignalsTestData.To,
            TestContext.Current.CancellationToken);

        episodes.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMetricHistory_WhenTelemetryIsUnavailable_ThrowsProviderUnavailableAndLeavesTheDownstreamScope()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, detail: "down"));

        var act = () => _signals.GetMetricHistoryAsync(
            SignalsTestData.MachineId,
            SignalsTestData.MetricId,
            SignalsTestData.From,
            SignalsTestData.To,
            bucketCount: 120,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSignalEpisodes_WhenTheDeadlineIsExceeded_ThrowsProviderUnavailable()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.DeadlineExceeded, detail: "slow"));

        var act = () => _signals.GetSignalEpisodesAsync(
            SignalsTestData.MachineId,
            SignalsTestData.From,
            SignalsTestData.To,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task GetSignalEpisodes_WhenTelemetryRejectsTheArguments_KeepsTheRpcException()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.InvalidArgument, detail: "bad range"));

        var act = () => _signals.GetSignalEpisodesAsync(
            SignalsTestData.MachineId,
            SignalsTestData.From,
            SignalsTestData.To,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RpcException>();
    }

    [Fact]
    public async Task GetSignalEpisodes_Always_EntersTheDownstreamScopeForTheCallOnly()
    {
        _invoker.Respond = _ => SignalsTestData.CreateEpisodesReply(hasOptionalValues: false);

        await _signals.GetSignalEpisodesAsync(
            SignalsTestData.MachineId,
            SignalsTestData.From,
            SignalsTestData.To,
            TestContext.Current.CancellationToken);

        _invoker.ScopesAtCall.Should().ContainSingle().Which.Should().Equal(SignalsTestData.DownstreamScope);
        _holder.Current.Should().BeEmpty();
    }
}
