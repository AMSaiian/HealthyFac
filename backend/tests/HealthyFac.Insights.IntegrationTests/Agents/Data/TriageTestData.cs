namespace HealthyFac.Insights.IntegrationTests.Agents.Data;

public static class TriageTestData
{
    public const string TriageModel = "test-triage-model";
    public const string PromptVersion = "triage-v1";
    public const int InputTokens = 120;
    public const int OutputTokens = 30;
    public const string Reason = "The spindle temperature stays above its limit.";
    public const int ProviderCallCount = 4;
    public const int MaxClockSteps = 300;

    public static string Answer(string verdict) =>
        $$"""{ "verdict": "{{verdict}}", "reason": "{{Reason}}" }""";
}
