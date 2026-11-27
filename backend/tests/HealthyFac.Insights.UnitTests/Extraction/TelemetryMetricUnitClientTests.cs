using Grpc.Core;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.MetricUnits;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Extraction;

[Feature(TestFeatures.Ingest)]
public sealed class TelemetryMetricUnitClientTests
{
    private readonly ScriptedDefinitionsClients _clients;
    private readonly TelemetryMetricUnitClient _client;

    public TelemetryMetricUnitClientTests()
    {
        var holder = new DownstreamScopeHolder();
        _clients = new ScriptedDefinitionsClients(holder);
        _client = new TelemetryMetricUnitClient(_clients, holder);
    }

    [Fact]
    public async Task Fetch_WhenTelemetryIsUnavailable_ThrowsProviderUnavailable()
    {
        _clients.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, detail: "down"));

        var act = () => _client.FetchAsync(TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task Fetch_WhenTheCallerCancelsTheCall_ThrowsOperationCanceled()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _clients.Respond = _ => throw new RpcException(new Status(StatusCode.Cancelled, detail: "cancelled"));

        var act = () => _client.FetchAsync(cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
