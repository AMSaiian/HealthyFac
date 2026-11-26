using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class ChatSendRouteTestData
{
    public const string CreatePath = "/api/insights/v1/chats";
    public const string SendPath = "/api/insights/v1/chats/abc/messages";
    public const string SearchPath = "/api/insights/v1/chats/search";
    public const string ReadPath = "/api/insights/v1/files/abc/ingest-attempts";
    public const int SendPermitLimit = 10;
    public const string MessagesPath = "/api/insights/v1/chats/abc/messages";

    /// <summary>The path of the POST-only send route that a browser also calls with GET (the message list).</summary>
    public static TheoryData<string> GetPreflightPaths { get; } = [MessagesPath];

    /// <summary>The case name, the method, and the path of a chat route that the send limit never counts.</summary>
    public static TheoryData<string, HttpMethod, string> OtherRoutes => new()
    {
        { "Search", HttpMethod.Post, SearchPath },
        { "MessagesRead", HttpMethod.Get, MessagesPath },
    };

    public static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(seconds: 3);

    public static IReadOnlyDictionary<string, string> Timeouts { get; } = new Dictionary<string, string>
    {
        ["ReverseProxy:Clusters:insights:HttpRequest:ActivityTimeout"] = "00:00:01",
        ["ReverseProxy:Clusters:insights-chat:HttpRequest:ActivityTimeout"] = "00:00:10",
    };

    public static IReadOnlyDictionary<string, string> Addresses(string address) =>
        new Dictionary<string, string>
        {
            [GatewaySettingKeys.InsightsAddress] = address,
            [GatewaySettingKeys.InsightsHealthAddress] = address,
            [GatewaySettingKeys.InsightsChatAddress] = address,
            [GatewaySettingKeys.InsightsChatHealthAddress] = address,
        };
}
