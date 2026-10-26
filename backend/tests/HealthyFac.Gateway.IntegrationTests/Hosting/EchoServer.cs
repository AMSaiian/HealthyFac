using System.Collections.Concurrent;
using HealthyFac.Shared.Web.Proxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Runs a small Kestrel app on a free port. It stands in for Core: it answers every request with the path and
/// query that it received, and it records each request, so a test sees what the gateway forwarded.
/// The readiness route is not recorded, because the gateway health checks call it. A test switches its answer
/// through <see cref="IsReady"/>.
/// </summary>
public sealed class EchoServer : IAsyncDisposable
{
    private const string LoopbackUrl = "http://127.0.0.1:0";
    private const string LoopbackNetwork = "127.0.0.0/8";
    private const string ReadinessPath = "/health/ready";

    private readonly bool _useForwardedHeaders;
    private readonly TimeSpan _responseDelay;
    private readonly ConcurrentQueue<EchoedRequest> _requests = new();

    private WebApplication? _app;
    private volatile bool _isReady = true;

    /// <param name="useForwardedHeaders">
    /// Applies the shared forwarded headers middleware and trusts the loopback network, like a service behind the
    /// gateway. The echoed path then includes the <c>PathBase</c> that the prefix header sets.
    /// </param>
    /// <param name="responseDelay">The time that the server waits before it answers a request.</param>
    public EchoServer(bool useForwardedHeaders = false, TimeSpan? responseDelay = null)
    {
        _useForwardedHeaders = useForwardedHeaders;
        _responseDelay = responseDelay ?? TimeSpan.Zero;
    }

    public string Address { get; private set; } = string.Empty;

    public IReadOnlyCollection<EchoedRequest> Requests => _requests;

    public int RequestCount => _requests.Count;

    public bool IsReady
    {
        get => _isReady;
        set => _isReady = value;
    }

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LoopbackUrl);

        if (_useForwardedHeaders)
        {
            builder.Configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [$"{ProxyOptions.SectionName}:KnownNetworks:0"] = LoopbackNetwork,
                });
            builder.Services.AddSharedForwardedHeaders(builder.Configuration);
        }

        _app = builder.Build();
        _app.Use((context, next) =>
        {
            if (context.Request.Path == ReadinessPath)
            {
                context.Response.StatusCode = _isReady
                    ? StatusCodes.Status200OK
                    : StatusCodes.Status503ServiceUnavailable;

                return Task.CompletedTask;
            }

            _requests.Enqueue(
                new EchoedRequest
                {
                    Path = context.Request.Path,
                    Query = context.Request.QueryString.Value ?? string.Empty,
                    Headers = context.Request.Headers.ToDictionary(
                        header => header.Key,
                        header => header.Value.ToString(),
                        StringComparer.OrdinalIgnoreCase),
                });

            return next();
        });

        if (_useForwardedHeaders)
        {
            _app.UseSharedForwardedHeaders();
        }

        _app.Run(async context =>
        {
            await Task.Delay(_responseDelay, context.RequestAborted);

            await context.Response.WriteAsync(
                UriHelper.BuildRelative(context.Request.PathBase, context.Request.Path, context.Request.QueryString));
        });

        await _app.StartAsync();

        Address = _app.Services
            .GetRequiredService<IServer>()
            .Features.Get<IServerAddressesFeature>()!
            .Addresses.Single();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.DisposeAsync();
        }
    }
}
