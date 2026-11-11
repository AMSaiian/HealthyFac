using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using HealthyFac.Shared.Web.Rpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace HealthyFac.Shared.IntegrationTests.Rpc;

public sealed class GrpcPortTests : IAsyncLifetime
{
    private const string CallPath = "/test.Service/Call";
    private const string PingPath = "/ping";

    private readonly int _apiPort = FreePort();
    private readonly int _grpcPort = FreePort();
    private WebApplication _app = null!;

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                [key: "HTTP_PORTS"] = $"{_apiPort};{_grpcPort}",
                [$"{GrpcEndpointOptions.SectionName}:Port"] = _grpcPort.ToString(),
            });
        builder.Services.AddSharedGrpc(builder.Environment, builder.Configuration);
        _app = builder.Build();
        _app.UseSharedGrpcPort();
        _app.MapPost(CallPath, () => Results.Ok());
        _app.MapGet(PingPath, () => Results.Ok());

        await _app.StartAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    [Fact]
    public async Task Call_OnGrpcPort_ReachesTheService()
    {
        var response = await SendAsync(HttpMethod.Post, CallPath, _grpcPort, isGrpcCall: true);

        response.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Call_OnApiPort_ReturnsNotFound()
    {
        var response = await SendAsync(HttpMethod.Post, CallPath, _apiPort, isGrpcCall: true);

        response.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HttpRoute_OnGrpcPort_ReturnsNotFound()
    {
        var response = await SendAsync(HttpMethod.Get, PingPath, _grpcPort, isGrpcCall: false);

        response.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HttpRoute_OnApiPort_ReturnsOk()
    {
        var response = await SendAsync(HttpMethod.Get, PingPath, _apiPort, isGrpcCall: false);

        response.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpStatusCode> SendAsync(HttpMethod method, string path, int port, bool isGrpcCall)
    {
        using var client = new HttpClient();
        using var request = new HttpRequestMessage(method, $"http://127.0.0.1:{port}{path}")
        {
            Version = port == _grpcPort ? HttpVersion.Version20 : HttpVersion.Version11,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
        };

        if (isGrpcCall)
        {
            request.Content = new ByteArrayContent([]);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue(GrpcConstants.ContentType);
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        return response.StatusCode;
    }

    private static int FreePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }
}
