namespace HealthyFac.Insights.IntegrationTests.Agents.Data;

public static class AgentTestData
{
    public const int LeaseSeconds = 300;

    /// <summary>The seconds that pass the lease and the debounce of the settings of the tests.</summary>
    public const int PastLeaseSeconds = 301;

    /// <summary>Two of these seconds pass the lease. One of them does not.</summary>
    public const int HalfLeaseSeconds = 200;
}
