using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.SignalEpisodes.Data;
using HealthyFac.Telemetry.Contracts.Signals;
using HealthyFac.Testing.Authentication;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.SignalEpisodes;

[Collection(TelemetryApiCollection.Name)]
[Feature(TestFeatures.Evaluation)]
public sealed class SignalGrpcTests(TelemetryApiFixture fixture) : IAsyncLifetime
{
    private readonly TelemetryApiFixture _fixture = fixture;
    private TelemetryGrpcHost _host = null!;

    public ValueTask InitializeAsync()
    {
        _host = _fixture.CreateGrpcHost();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task GetMetricHistory_WithTheServiceTokenAndActorHeaders_ReturnsTheBuckets()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var reply = await _host.Client.GetMetricHistoryAsync(
            HistoryRequest(arrangement, SignalApiTestData.BucketCount),
            ServiceHeaders(SignalApiTestData.ReadPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Buckets.Select(bucket => (bucket.Avg, bucket.Min, bucket.Max, bucket.Count))
            .Should()
            .Equal((15d, 10d, 20d, 2L), (30d, 30d, 30d, 1L));
    }

    [Fact]
    public async Task GetSignalEpisodes_WithTheServiceTokenAndActorHeaders_ReturnsTheEpisodes()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var reply = await _host.Client.GetSignalEpisodesAsync(
            EpisodesRequest(arrangement),
            ServiceHeaders(SignalApiTestData.ReadPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var episode = reply.Episodes.Should().ContainSingle().Subject;
        episode.Rule.Should().Be(expected: "threshold");
        episode.MetricKey.Should().Be(SignalApiTestData.MetricKey);
        episode.Value.Should().Be(expected: 91);
        episode.HasStatusDefinitionId.Should().BeFalse();
    }

    [Fact]
    public async Task GetMetricHistory_WithoutTheReadPermission_ReturnsPermissionDenied()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Client.GetMetricHistoryAsync(
            HistoryRequest(arrangement, SignalApiTestData.BucketCount),
            ServiceHeaders(SignalApiTestData.OtherPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetMetricHistory_WithAnotherCompany_ReturnsNotFound()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Client.GetMetricHistoryAsync(
            HistoryRequest(arrangement, SignalApiTestData.BucketCount),
            ServiceHeaders(SignalApiTestData.ReadPermission, Guid.CreateVersion7()),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetMetricHistory_WithAnInvalidCount_ReturnsInvalidArgument()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Client.GetMetricHistoryAsync(
            HistoryRequest(arrangement, bucketCount: 0),
            ServiceHeaders(SignalApiTestData.ReadPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    private Metadata ServiceHeaders(string permission, Guid? companyId = null)
    {
        var token = _fixture.Tokens.CreateServiceToken(
            new TestServiceTokenClaims
            {
                ClientId = TelemetryApiTestData.InsightsClientId,
                Permissions = [permission],
            });

        return TelemetryGrpcHost.BearerHeaders(token, Guid.CreateVersion7(), companyId ?? _fixture.CompanyId);
    }

    private static GetMetricHistoryRequest HistoryRequest(SignalArrangement arrangement, int bucketCount) =>
        new()
        {
            MachineId = arrangement.MachineId.ToString(),
            MetricDefinitionId = arrangement.MetricId.ToString(),
            From = Timestamp.FromDateTime(SignalApiTestData.RangeStart),
            To = Timestamp.FromDateTime(SignalApiTestData.RangeEnd),
            BucketCount = bucketCount,
        };

    private static GetSignalEpisodesRequest EpisodesRequest(SignalArrangement arrangement) =>
        new()
        {
            MachineId = arrangement.MachineId.ToString(),
            From = Timestamp.FromDateTime(SignalApiTestData.RangeStart),
            To = Timestamp.FromDateTime(SignalApiTestData.RangeEnd),
        };
}
