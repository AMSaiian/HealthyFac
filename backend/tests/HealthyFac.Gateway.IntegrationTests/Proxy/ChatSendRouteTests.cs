using System.Net;
using HealthyFac.Gateway.IntegrationTests.CrossOrigin.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
[Feature(TestFeatures.Chat)]
[Feature(TestFeatures.Gateway)]
public sealed class ChatSendRouteTests(GatewayEnvironment environment)
{
    private readonly GatewayEnvironment _environment = environment;

    [Fact]
    public async Task ChatSend_WithASlowReply_PassesWhileAnotherInsightsRouteTimesOut()
    {
        await using var insights = new EchoServer(responseDelay: ChatSendRouteTestData.SlowResponseDelay);
        await insights.StartAsync();
        await using var gateway = CreateGateway(insights, ChatSendRouteTestData.Timeouts);
        using var client = gateway.CreateAuthenticatedClient();

        using var send = await client.PostAsync(
            ChatSendRouteTestData.SendPath, content: null, TestContext.Current.CancellationToken);

        using var read = await client.GetAsync(ChatSendRouteTestData.ReadPath, TestContext.Current.CancellationToken);

        send.StatusCode.Should().Be(HttpStatusCode.OK);
        read.StatusCode.Should().Be(HttpStatusCode.GatewayTimeout);
    }

    [Fact]
    public async Task ChatSend_TheEleventhSendInTheWindow_Returns429()
    {
        await using var insights = new EchoServer();
        await insights.StartAsync();
        await using var gateway = CreateGateway(insights, settings: null);
        using var client = gateway.CreateAuthenticatedClient();

        for (var attempt = 0; attempt < ChatSendRouteTestData.SendPermitLimit; attempt++)
        {
            var path = attempt % 2 == 0 ? ChatSendRouteTestData.CreatePath : ChatSendRouteTestData.SendPath;
            using var allowed = await client.PostAsync(path, content: null, TestContext.Current.CancellationToken);
        }

        using var response = await client.PostAsync(
            ChatSendRouteTestData.SendPath, content: null, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Theory]
    [MemberData(nameof(ChatSendRouteTestData.OtherRoutes), MemberType = typeof(ChatSendRouteTestData))]
    public async Task ChatRoute_AfterManyRequests_IsNotCountedAgainstTheSendLimit(
        string caseName,
        HttpMethod method,
        string path
    )
    {
        await using var insights = new EchoServer();
        await insights.StartAsync();
        await using var gateway = CreateGateway(insights, settings: null);
        using var client = gateway.CreateAuthenticatedClient();

        for (var attempt = 0; attempt <= ChatSendRouteTestData.SendPermitLimit; attempt++)
        {
            using var request = new HttpRequestMessage(method, path);
            using var other = await client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        using var send = await client.PostAsync(
            ChatSendRouteTestData.SendPath, content: null, TestContext.Current.CancellationToken);

        send.StatusCode.Should().Be(HttpStatusCode.OK, caseName);
    }

    [Theory]
    [MemberData(nameof(ChatSendRouteTestData.GetPreflightPaths), MemberType = typeof(ChatSendRouteTestData))]
    public async Task ChatPreflight_ForAGetOnAPostRoutePath_Returns204WithAllowOrigin(string path)
    {
        await using var insights = new EchoServer();
        await insights.StartAsync();
        await using var gateway = CreateGateway(insights, settings: null);
        using var client = gateway.CreateClient();

        using var response = await client.SendPreflightAsync(path, requestedMethod: "GET");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        response.Headers.GetValues(GatewayCorsTestData.AllowOriginHeader)
            .Should()
            .Equal(GatewayFactory.DefaultCorsOrigin);

        insights.RequestCount.Should().Be(expected: 0);
    }

    private GatewayFactory CreateGateway(EchoServer insights, IReadOnlyDictionary<string, string>? settings)
    {
        var merged = new Dictionary<string, string>(ChatSendRouteTestData.Addresses(insights.Address));

        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            merged[key] = value;
        }

        return _environment.CreateGateway(insights, settings: merged);
    }
}
