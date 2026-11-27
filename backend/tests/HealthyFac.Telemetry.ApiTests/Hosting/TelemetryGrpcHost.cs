using System.Net;
using System.Net.Sockets;
using Grpc.Core;
using Grpc.Net.Client;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Telemetry.Contracts.Definitions;
using HealthyFac.Telemetry.Contracts.Signals;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.ApiTests.Hosting;

/// <summary>
/// Runs the Telemetry host on real Kestrel ports: the API port with HTTP/1.1 and the gRPC port with HTTP/2 without TLS.
/// The in-memory test server has no ports, so it cannot test the port rules.
/// </summary>
public sealed class TelemetryGrpcHost : IAsyncDisposable
{
    private readonly TelemetryApiFactory _factory;
    private readonly GrpcChannel _channel;

    public TelemetryGrpcHost(IReadOnlyDictionary<string, string?> settings, TestTokenFactory tokens)
    {
        var apiPort = FreePort();
        var grpcPort = FreePort();
        var hostSettings = new Dictionary<string, string?>(settings) { ["Grpc:Port"] = grpcPort.ToString() };

        _factory = new TelemetryApiFactory(
            hostSettings,
            tokens,
            services => services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Loopback, apiPort);
                options.Listen(IPAddress.Loopback, grpcPort);
            }));

        _factory.UseKestrel();
        _factory.StartServer();

        _channel = GrpcChannel.ForAddress($"http://127.0.0.1:{grpcPort}");
        Client = new TelemetrySignals.TelemetrySignalsClient(_channel);
        Definitions = new TelemetryDefinitions.TelemetryDefinitionsClient(_channel);
    }

    public TelemetrySignals.TelemetrySignalsClient Client { get; }

    public TelemetryDefinitions.TelemetryDefinitionsClient Definitions { get; }

    public async ValueTask DisposeAsync()
    {
        _channel.Dispose();
        await _factory.DisposeAsync();
    }

    public static Metadata BearerHeaders(string token, Guid? actorId = null, Guid? companyId = null)
    {
        var headers = new Metadata { { "Authorization", $"Bearer {token}" } };

        if (actorId is not null && companyId is not null)
        {
            headers.Add(ActorHeaderNames.ActorId, actorId.Value.ToString());
            headers.Add(ActorHeaderNames.CompanyId, companyId.Value.ToString());
        }

        return headers;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
