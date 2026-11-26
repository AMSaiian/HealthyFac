using System.Diagnostics.Metrics;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using HealthyFac.Gateway.IntegrationTests.Realtime.Data;
using HealthyFac.Shared.Web.Metrics;
using HealthyFac.Shared.Web.Realtime;
using HealthyFac.Testing.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Core;

namespace HealthyFac.Gateway.IntegrationTests.Realtime;

[Collection(GatewayProxyCollection.Name)]
[Feature(TestFeatures.Gateway)]
[Feature(TestFeatures.Realtime)]
public sealed class RealtimeObservabilityTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string ConnectionsMeter = "Microsoft.AspNetCore.Http.Connections";

    private readonly GatewayEnvironment _environment = environment;

    private readonly EchoServer _core = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync() => await _core.StartAsync();

    public ValueTask DisposeAsync() => _core.DisposeAsync();

    [Fact]
    public async Task Connect_WithTokenInQuery_LeavesTheTokenOutOfLogsAndSpans()
    {
        var spans = new SpanTagCapture();
        var logs = new LogTextCapture();
        await using var gateway = CreateGateway(spans, logs);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        var encodedToken = Uri.EscapeDataString(token);

        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);
        await client.DisposeAsync();
        await WaitForRequestLogAsync(logs);

        logs.Lines.Should().NotContain(line => line.Contains(token) || line.Contains(encodedToken));
        spans.TagValues.Should().NotContain(tag => tag.Contains(token) || tag.Contains(encodedToken));
    }

    [Fact]
    public async Task Connect_WithTokenInQuery_RecordsTheRequestInLogsAndSpans()
    {
        var spans = new SpanTagCapture();
        var logs = new LogTextCapture();
        await using var gateway = CreateGateway(spans, logs);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));

        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);
        await client.DisposeAsync();
        await WaitForRequestLogAsync(logs);

        logs.Lines.Should().Contain(line => line.Contains("responded") && line.Contains(RealtimeConstants.HubPath));
        spans.TagValues.Should().Contain(tag => tag.Contains(RealtimeConstants.HubPath));
    }

    [Fact]
    public async Task Connect_AfterTheStaticLoggerChanged_StillRecordsTheRequestLog()
    {
        var logs = new LogTextCapture();
        await using var gateway = CreateGateway(new SpanTagCapture(), logs);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        _ = gateway.Services;
        Log.Logger = Logger.None;

        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);
        await client.DisposeAsync();
        await WaitForRequestLogAsync(logs);

        logs.Lines.Should().Contain(line => IsHubRequestLog(line));
    }

    [Fact]
    public async Task Connect_AfterStart_PublishesTheConnectionsMeter()
    {
        var publishedMeters = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, _) => publishedMeters.Add(instrument.Meter.Name);
        await using var gateway = _environment.CreateGateway(_core);
        var token = gateway.Tokens.CreateToken(RealtimeTestData.Claims(Guid.NewGuid()));
        await using var client = await RealtimeTestClient.ConnectAsync(gateway, token);

        listener.Start();
        var additionalMeters = gateway.Services.GetRequiredService<IOptions<MetricsOptions>>().Value.AdditionalMeters;

        publishedMeters.Should().Contain(ConnectionsMeter);
        additionalMeters.Should().Contain(ConnectionsMeter);
    }

    private static Task WaitForRequestLogAsync(LogTextCapture logs) =>
        GatewayLoggerFactory.WaitForLineAsync(logs, IsHubRequestLog);

    private static bool IsHubRequestLog(string line) =>
        line.Contains(value: "responded") && line.Contains(RealtimeConstants.HubPath);

    private GatewayFactory CreateGateway(SpanTagCapture spans, LogTextCapture logs) =>
        _environment.CreateGateway(
            _core,
            settings: new Dictionary<string, string> { [GatewaySettingKeys.TracingEnabled] = "true" },
            configureServices: services =>
            {
                services.ConfigureOpenTelemetryTracerProvider(tracing => tracing.AddProcessor(spans));
                GatewayLoggerFactory.CaptureHostLogs(services, logs);
            });
}
