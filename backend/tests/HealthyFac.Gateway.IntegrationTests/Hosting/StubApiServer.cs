using System.Collections.Concurrent;
using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Gateway.IntegrationTests.Hosting;

/// <summary>
/// Runs a small Kestrel app on a free port. It stands in for a service that the gateway calls itself: it answers
/// the configured JSON for a method and a path, answers <c>404</c> for any other route, and records each request.
/// The readiness route is not recorded, because the gateway health checks call it.
/// </summary>
public sealed class StubApiServer : IAsyncDisposable
{
    private const string LoopbackUrl = "http://127.0.0.1:0";
    private const string ReadinessPath = "/health/ready";

    private readonly ConcurrentQueue<EchoedRequest> _requests = new();
    private readonly ConcurrentDictionary<(string Method, string Path), Func<string, StubResponse>> _responses = new();

    private WebApplication? _app;

    public string Address { get; private set; } = string.Empty;

    public IReadOnlyCollection<EchoedRequest> Requests => _requests;

    /// <summary>Sets the answer for a method and a path (the path without the query).</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path that the server receives.</param>
    /// <param name="status">The status code of the answer.</param>
    /// <param name="json">The JSON body of the answer.</param>
    public void Respond(HttpMethod method, string path, int status, string json) =>
        _responses[(method.Method, path)] = _ => new StubResponse(status, json);

    /// <summary>Sets a computed answer for a method and a path. The function gets the request body.</summary>
    /// <param name="method">The HTTP method.</param>
    /// <param name="path">The path that the server receives.</param>
    /// <param name="answer">Builds the status code and the JSON body from the request body.</param>
    public void Respond(HttpMethod method, string path, Func<string, (int Status, string Json)> answer) =>
        _responses[(method.Method, path)] = requestBody =>
        {
            var (status, json) = answer(requestBody);

            return new StubResponse(status, json);
        };

    public async Task StartAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LoopbackUrl);
        _app = builder.Build();
        _app.Run(HandleAsync);

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

    private async Task HandleAsync(HttpContext context)
    {
        if (context.Request.Path == ReadinessPath)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;

            return;
        }

        using var reader = new StreamReader(context.Request.Body);
        var body = await reader.ReadToEndAsync(context.RequestAborted);

        _requests.Enqueue(
            new EchoedRequest
            {
                BodyLength = body.Length,
                Body = body,
                Method = context.Request.Method,
                Path = context.Request.Path,
                Query = context.Request.QueryString.Value ?? string.Empty,
                Headers = context.Request.Headers.ToDictionary(
                    header => header.Key,
                    header => header.Value.ToString(),
                    StringComparer.OrdinalIgnoreCase),
            });

        var key = (context.Request.Method, context.Request.Path.Value ?? string.Empty);

        if (!_responses.TryGetValue(key, out var factory))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        var answer = factory(body);

        context.Response.StatusCode = answer.Status;
        context.Response.ContentType = MediaTypeNames.Application.Json;
        await context.Response.WriteAsync(answer.Json, context.RequestAborted);
    }

    private sealed record StubResponse(int Status, string Json);
}
