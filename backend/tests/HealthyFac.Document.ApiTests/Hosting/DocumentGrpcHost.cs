using System.Net;
using System.Net.Sockets;
using Grpc.Core;
using Grpc.Net.Client;
using HealthyFac.Document.Contracts.Files;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Authentication;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.ApiTests.Hosting;

/// <summary>
/// Runs the Document host on real Kestrel ports: the API port with HTTP/1.1 and the gRPC port with HTTP/2 without TLS.
/// The in-memory test server has no ports, so it cannot test the port rules.
/// </summary>
public sealed class DocumentGrpcHost : IAsyncDisposable
{
    public const string AllowedClientId = "healthyfac-insights";

    private readonly DocumentApiFactory _factory;
    private readonly GrpcChannel _channel;

    public DocumentGrpcHost(IReadOnlyDictionary<string, string?> settings, TestTokenFactory tokens)
    {
        ApiPort = FreePort();
        GrpcPort = FreePort();

        var hostSettings = new Dictionary<string, string?>(settings)
        {
            [key: "Grpc:Port"] = GrpcPort.ToString(),
            [key: "ServiceActors:AllowedClients:0"] = AllowedClientId,
        };
        _factory = new DocumentApiFactory(
            hostSettings,
            tokens,
            services => services.Configure<KestrelServerOptions>(options =>
            {
                options.Listen(IPAddress.Loopback, ApiPort);
                options.Listen(IPAddress.Loopback, GrpcPort);
            }));
        _factory.UseKestrel();
        _factory.StartServer();

        _channel = GrpcChannel.ForAddress($"http://127.0.0.1:{GrpcPort}");
        Client = new DocumentFiles.DocumentFilesClient(_channel);
    }

    public int ApiPort { get; }

    public int GrpcPort { get; }

    public DocumentFiles.DocumentFilesClient Client { get; }

    public HttpClient CreateApiClient() => new() { BaseAddress = new Uri($"http://127.0.0.1:{ApiPort}") };

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
