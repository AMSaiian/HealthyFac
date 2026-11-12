namespace HealthyFac.Core.UnitTests.WorkOrders.Scheduling.Data;

public static class SlotRulesTestData
{
    public static TheoryData<string, string, int> SlotsInsideOneBlock => new()
    {
        { "InsideBlock", "09:00", 60 },
        { "UpToBlockEnd", "11:00", 60 },
    };
}
