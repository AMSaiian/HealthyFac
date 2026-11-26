using System.Net;
using System.Net.Mime;
using System.Text;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayProxyCollection.Name)]
public sealed class CompositionFailureTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;
    private readonly CompositionStubs _stubs = new();

    public async ValueTask InitializeAsync() => await _stubs.StartAsync();

    public async ValueTask DisposeAsync() => await _stubs.DisposeAsync();

    [Theory]
    [MemberData(nameof(CompositionFailureTestData.Endpoints), MemberType = typeof(CompositionFailureTestData))]
    public async Task Endpoint_WithAStoppedTelemetry_Returns503(
        string caseName,
        string method,
        string path,
        string? body
    )
    {
        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();
        await _stubs.Telemetry.DisposeAsync();

        var response = await SendAsync(client, method, path, body);

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable, caseName);
    }

    [Fact]
    public async Task Dismiss_WhenTelemetryAnswers403_Returns403()
    {
        _stubs.Telemetry.Respond(
            HttpMethod.Post,
            CompositionStubs.TelemetryMachineDismiss,
            status: 403,
            CompositionFailureTestData.ForbiddenJson);

        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await SendAsync(client, method: "POST", CompositionStubs.PublicMachineDismiss, body: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Dismiss_WhenCoreAnswers404AndTelemetryAnswers403_Returns403()
    {
        _stubs.Core.Respond(
            HttpMethod.Post,
            CompositionStubs.CoreMachineDismiss,
            status: 404,
            CompositionFailureTestData.NotFoundJson);

        _stubs.Telemetry.Respond(
            HttpMethod.Post,
            CompositionStubs.TelemetryMachineDismiss,
            status: 403,
            CompositionFailureTestData.ForbiddenJson);

        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await SendAsync(client, method: "POST", CompositionStubs.PublicMachineDismiss, body: null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string method,
        string path,
        string? body
    )
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), path);

        if (body is not null)
        {
            request.Content = new StringContent(body, Encoding.UTF8, MediaTypeNames.Application.Json);
        }

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
