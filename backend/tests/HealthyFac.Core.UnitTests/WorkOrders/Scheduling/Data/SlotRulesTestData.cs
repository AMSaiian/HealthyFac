namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;

public static class SlotRulesTestData
{
    public static TheoryData<string, string, int> SlotsInsideOneBlock => new()
    {
        { "InsideBlock", "09:00", 60 },
        { "UpToBlockEnd", "11:00", 60 },
    };

    public static TheoryData<string, int, string, string> SlotsThatDoNotOverlap => new()
    {
        { "back to back", 2, "09:00", "10:00" },
        { "own occupancy", 1, "09:00", "09:30" },
    };

    public static TheoryData<string, string, string> Completions => new()
    {
        { "late completion blocks until completion", "10:40", "10:40" },
        { "early completion keeps the planned end", "09:20", "10:00" },
    };
}
