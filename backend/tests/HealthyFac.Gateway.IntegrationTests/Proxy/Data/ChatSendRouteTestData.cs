using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Proxy.Data;

public static class ChatSendRouteTestData
{
    public const string CreatePath = "/api/insights/v1/chats";
    public const string SendPath = "/api/insights/v1/chats/abc/messages";
    public const string SearchPath = "/api/insights/v1/chats/search";
    public const string ReadPath = "/api/insights/v1/files/abc/ingest-attempts";
    public const int SendPermitLimit = 10;

    public static readonly TimeSpan SlowResponseDelay = TimeSpan.FromSeconds(seconds: 3);

    public static IReadOnlyDictionary<string, string> Timeouts { get; } = new Dictionary<string, string>
    {
        [key: "ReverseProxy:Clusters:insights:HttpRequest:ActivityTimeout"] = "00:00:01",
        [key: "ReverseProxy:Clusters:insights-chat:HttpRequest:ActivityTimeout"] = "00:00:10",
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
