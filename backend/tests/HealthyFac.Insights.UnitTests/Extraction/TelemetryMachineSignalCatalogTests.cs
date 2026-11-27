using Grpc.Core;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Extraction;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Telemetry.Contracts.Definitions;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Extraction;

[Feature(TestFeatures.Ingest)]
public sealed class TelemetryMachineSignalCatalogTests
{
    private static readonly Guid TempId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");
    private static readonly Guid StatusId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000301");
    private static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    private readonly DownstreamScopeHolder _holder = new();
    private readonly ScriptedDefinitionsClients _clients;
    private readonly TelemetryMachineSignalCatalog _catalog;

    public TelemetryMachineSignalCatalogTests()
    {
        _clients = new ScriptedDefinitionsClients(_holder);
        _catalog = new TelemetryMachineSignalCatalog(_clients, _holder);
    }

    [Fact]
    public async Task Get_ForAMachine_ReadsTheMetricKeysAndTheStatusCodesWithTheirIds()
    {
        var reply = new ListDefinitionsReply();
        reply.Metrics.Add(new MetricDefinition { Id = TempId.ToString(), Key = "temp" });
        reply.Statuses.Add(new StatusDefinition { Id = StatusId.ToString(), Code = "E1" });

        _clients.Respond = request =>
        {
            ((ListDefinitionsRequest)request).MachineId.Should().Be(MachineId.ToString());

            return reply;
        };

        var signals = await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        signals.Metrics.Should().Equal(new SignalReference { Id = TempId, Key = "temp" });
        signals.Statuses.Should().Equal(new SignalReference { Id = StatusId, Key = "E1" });
    }

    [Fact]
    public async Task Get_Always_EntersTheScopeForTheCallOnly()
    {
        _clients.Respond = _ => new ListDefinitionsReply();

        await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        _clients.ScopesPerCall.Should().ContainSingle().Which.Should().Equal("telemetry");
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WhenTelemetryHasNoMachine_ReturnsEmptyLists()
    {
        _clients.Respond = _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "none"));

        var signals = await _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        signals.MetricKeys.Should().BeEmpty();
        signals.StatusCodes.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_WhenTelemetryIsUnavailable_ThrowsProviderUnavailable()
    {
        _clients.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, detail: "down"));

        var act = () => _catalog.GetAsync(MachineId, TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task Get_WhenTheCallerCancelsTheCall_ThrowsOperationCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _clients.Respond = _ => throw new RpcException(new Status(StatusCode.Cancelled, detail: "cancelled"));

        var act = () => _catalog.GetAsync(MachineId, cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
