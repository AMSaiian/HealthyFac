using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.Rpc.Data;
using HealthyFac.Telemetry.ApiTests.SignalEpisodes.Data;
using HealthyFac.Telemetry.Contracts.Definitions;
using HealthyFac.Telemetry.Contracts.Signals;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Telemetry.ApiTests.Rpc;

[Collection(TelemetryApiCollection.Name)]
public sealed class TelemetryRpcTests(TelemetryApiFixture fixture) : IAsyncLifetime
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
    public async Task GetPhaseTimes_ForAMachineWithoutPhases_ReturnsTheWholeRangeAsTimeWithoutAPhase()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var reply = await _host.Client.GetPhaseTimesAsync(
            PhaseTimesRequest(arrangement.MachineId),
            Headers([TelemetryRpcTestData.SignalsRead]),
            cancellationToken: TestContext.Current.CancellationToken);

        (reply.Phases.Sum(phase => phase.Seconds) + reply.UnknownModeSeconds + reply.NoDataSeconds)
            .Should()
            .BeApproximately(TelemetryRpcTestData.RangeSeconds, precision: 1);
    }

    [Fact]
    public async Task GetPhaseTimes_WithoutTheSignalsPermission_ReturnsPermissionDenied()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Client.GetPhaseTimesAsync(
            PhaseTimesRequest(arrangement.MachineId),
            Headers([TelemetryRpcTestData.MetricDefinitionsRead]),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetPhaseTimes_ForAMachineOfAnotherCompany_ReturnsNotFound()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Client.GetPhaseTimesAsync(
            PhaseTimesRequest(arrangement.MachineId),
            Headers([TelemetryRpcTestData.SignalsRead], Guid.CreateVersion7()),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task ListDefinitions_WithBothReadPermissions_ReturnsTheMetricsAndTheStatuses()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();
        await _fixture.ArrangeStatusDefinitionAsync(arrangement.MachineId);

        var reply = await _host.Definitions.ListDefinitionsAsync(
            new ListDefinitionsRequest { MachineId = arrangement.MachineId.ToString() },
            Headers([TelemetryRpcTestData.MetricDefinitionsRead, TelemetryRpcTestData.StatusDefinitionsRead]),
            cancellationToken: TestContext.Current.CancellationToken);

        var metric = reply.Metrics.Should().ContainSingle().Subject;
        var status = reply.Statuses.Should().ContainSingle().Subject;
        metric.Key.Should().Be(SignalApiTestData.MetricKey);
        metric.ValueKind.Should().Be(TelemetryRpcTestData.NumericKind);
        metric.Unit.Should().Be(TelemetryRpcTestData.MetricUnit);
        metric.Min.Should().Be(expected: -20);
        metric.Max.Should().Be(expected: 200);
        status.Code.Should().Be(TelemetryRpcTestData.StatusCode);
        status.Severity.Should().Be(TelemetryRpcTestData.StatusSeverity);
    }

    [Theory]
    [MemberData(nameof(TelemetryRpcTestData.DefinitionPermissionGaps), MemberType = typeof(TelemetryRpcTestData))]
    public async Task ListDefinitions_WithOneReadPermissionMissing_ReturnsPermissionDenied(
        string caseName,
        string[] permissions
    )
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Definitions.ListDefinitionsAsync(
            new ListDefinitionsRequest { MachineId = arrangement.MachineId.ToString() },
            Headers(permissions),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>(caseName);
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task ListDefinitions_ForAMachineOfAnotherCompany_ReturnsNotFound()
    {
        var arrangement = await _fixture.ArrangeSignalMachineAsync();

        var act = async () => await _host.Definitions.ListDefinitionsAsync(
            new ListDefinitionsRequest { MachineId = arrangement.MachineId.ToString() },
            Headers(
                [TelemetryRpcTestData.MetricDefinitionsRead, TelemetryRpcTestData.StatusDefinitionsRead],
                Guid.CreateVersion7()),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    private Metadata Headers(string[] permissions, Guid? companyId = null)
    {
        var token = _fixture.Tokens.CreateServiceToken(
            new TestServiceTokenClaims
            {
                ClientId = TelemetryApiTestData.InsightsClientId,
                Permissions = permissions,
            });

        return TelemetryGrpcHost.BearerHeaders(token, Guid.CreateVersion7(), companyId ?? _fixture.CompanyId);
    }

    private static GetPhaseTimesRequest PhaseTimesRequest(Guid machineId) =>
        new()
        {
            MachineId = machineId.ToString(),
            From = Timestamp.FromDateTime(TelemetryRpcTestData.RangeStart),
            To = Timestamp.FromDateTime(TelemetryRpcTestData.RangeEnd),
        };
}
