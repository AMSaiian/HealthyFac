namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;

public static class CompletionSlideTestData
{
    public static TheoryData<string, string, string> CompletionsThatMoveNothing => new()
    {
        { "on time", "09:00", "09:00" },
        { "early", "08:30", "09:00" },
        { "early with a staged order inside the freed window", "08:30", "08:15" },
    };
}
