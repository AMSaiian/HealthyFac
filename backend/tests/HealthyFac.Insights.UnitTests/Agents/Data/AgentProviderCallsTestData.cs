using System.Net;

namespace HealthyFac.Insights.UnitTests.Agents.Data;

public static class AgentProviderCallsTestData
{
    /// <summary>The text that a provider puts into its error. No message of ours may hold it.</summary>
    public const string ProviderText = "secret provider detail";

    /// <summary>The most 1 second steps that a test waits for the retries: more than the sum of the delays.</summary>
    public const int MaxSteps = 300;

    public static readonly IReadOnlyList<int> Delays = [10, 30, 90];

    public static readonly DateTimeOffset Start = new(
        year: 2026, month: 11, day: 9, hour: 8, minute: 0, second: 0, TimeSpan.Zero);

    public static HttpRequestException Transient() =>
        new(ProviderText, inner: null, HttpStatusCode.ServiceUnavailable);
}
