using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
public sealed class DocumentRouteTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string ForwardedPrefixHeader = "X-Forwarded-Prefix";
    private const string PublicPrefix = "/api/document";

    private readonly GatewayEnvironment _environment = environment;
    private readonly EchoServer _core = new(useForwardedHeaders: true);
    private readonly EchoServer _document = new(useForwardedHeaders: true);

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _document.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _document.DisposeAsync();
    }

    [Fact]
    public async Task DocumentRoute_WithVersionedPath_Forwards()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            requestUri: "/api/document/v1/files/links", TestContext.Current.CancellationToken);

        var forwardedRequest = _document.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.Path.Should().Be(expected: "/v1/files/links");
        forwardedRequest.Headers[ForwardedPrefixHeader].Should().Be(PublicPrefix);
        _core.RequestCount.Should().Be(expected: 0);
    }

    [Theory]
    [MemberData(nameof(DocumentRouteTestData.UnversionedPaths), MemberType = typeof(DocumentRouteTestData))]
    public async Task DocumentRoute_WithUnversionedPath_DoesNotForward(string path)
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(path, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);

        _document.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task DocumentRoute_WithBodyAboveCoreLimit_Forwards()
    {
        await using var gateway = CreateGateway();
        gateway.UseKestrel();
        using var client = gateway.CreateAuthenticatedClient();
        using var body = new ZeroBodyContent(DocumentRouteTestData.BodyAboveCoreLimitBytes);

        var response = await client.PostAsync(
            DocumentRouteTestData.UploadPath, body, TestContext.Current.CancellationToken);

        var forwardedRequest = _document.Requests.Single();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        forwardedRequest.BodyLength.Should().Be(DocumentRouteTestData.BodyAboveCoreLimitBytes);
    }

    [Fact]
    public async Task CoreRoute_WithBodyAboveKestrelLimit_Returns413()
    {
        await using var gateway = CreateGateway();
        gateway.UseKestrel();
        using var client = gateway.CreateAuthenticatedClient();
        using var body = new ZeroBodyContent(DocumentRouteTestData.BodyAboveCoreLimitBytes);

        var response = await client.PostAsync(
            DocumentRouteTestData.CoreUploadPath, body, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);

        _core.RequestCount.Should().Be(expected: 0);
    }

    [Fact]
    public async Task DocumentRoute_WithBodyAbove101MiB_Returns413()
    {
        await using var gateway = CreateGateway();
        gateway.UseKestrel();
        using var client = gateway.CreateAuthenticatedClient();
        using var body = new ZeroBodyContent(DocumentRouteTestData.BodyAboveRouteLimitBytes);
        using var request = new HttpRequestMessage(HttpMethod.Post, DocumentRouteTestData.UploadPath)
        {
            Content = body,
            Headers = { ExpectContinue = true },
        };

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);

        _document.RequestCount.Should().Be(expected: 0);
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(
            _core,
            settings: new Dictionary<string, string>
            {
                [GatewaySettingKeys.DocumentAddress] = _document.Address,
                [GatewaySettingKeys.DocumentHealthAddress] = _document.Address,
            });
}
