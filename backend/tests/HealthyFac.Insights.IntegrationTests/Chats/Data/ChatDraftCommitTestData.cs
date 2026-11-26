namespace HealthyFac.Insights.IntegrationTests.Chats.Data;

public static class ChatDraftCommitTestData
{
    /// <summary>The case name, the length of the owner name, and the length that the messages carry.</summary>
    public static TheoryData<string, int, int> OwnerNameLengths => new()
    {
        { "TooLong", 150, 100 },
        { "AtTheLimit", 100, 100 },
    };

    /// <summary>The case name and an owner name that holds no text.</summary>
    public static TheoryData<string, string?> OwnerNamesWithoutText => new()
    {
        { "Null", null },
        { "Blank", "   " },
    };
}
