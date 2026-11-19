using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.ApiTests.Cases.Data;

public static class CaseApiTestData
{
    public const string ReadPermission = "cases:read";
    public const string WritePermission = "cases:write";
    public const string BasePath = "/v1/cases";
    public const string SearchPath = "/v1/cases/search";
    public const string SearchBody = "{\"offset\":0,\"limit\":20,\"filter\":{}}";

    public static readonly DateTime Now = new(
        year: 2026,
        month: 11,
        day: 18,
        hour: 8,
        minute: 0,
        second: 0,
        DateTimeKind.Utc);

    public static string ByIdPath(string encryptedId) => $"{BasePath}/{encryptedId}";

    public static string SignalsSearchPath(string encryptedId) => $"{BasePath}/{encryptedId}/signals/search";

    public static string RunsSearchPath(string encryptedId) => $"{BasePath}/{encryptedId}/runs/search";

    public static string RunsPath(string encryptedId) => $"{BasePath}/{encryptedId}/runs";

    public static string ClosePath(string encryptedId) => $"{BasePath}/{encryptedId}/close";

    public static TheoryData<string, AgentRunErrorCode, bool> AiUnavailableCases =>
        new()
        {
            { "providerUnavailable", AgentRunErrorCode.ProviderUnavailable, true },
            { "unexpectedError", AgentRunErrorCode.UnexpectedError, true },
            { "caseChanged", AgentRunErrorCode.CaseChanged, false },
        };
}
