using System.Net;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using HealthyFac.Gateway.IntegrationTests.Proxy.Data;

namespace HealthyFac.Gateway.IntegrationTests.Proxy;

[Collection(GatewayProxyCollection.Name)]
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

    [Fact]
    public async Task ChatSearch_AfterManyRequests_IsNotCountedAgainstTheSendLimit()
    {
        await using var insights = new EchoServer();
        await insights.StartAsync();
        await using var gateway = CreateGateway(insights, settings: null);
        using var client = gateway.CreateAuthenticatedClient();
        for (var attempt = 0; attempt <= ChatSendRouteTestData.SendPermitLimit; attempt++)
        {
            using var search = await client.PostAsync(
                ChatSendRouteTestData.SearchPath, content: null, TestContext.Current.CancellationToken);
        }

        using var send = await client.PostAsync(
            ChatSendRouteTestData.SendPath, content: null, TestContext.Current.CancellationToken);

        send.StatusCode.Should().Be(HttpStatusCode.OK);
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
