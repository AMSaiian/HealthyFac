namespace HealthyFac.Shared.IntegrationTests.TestReset.Data;

public static class ResetTestData
{
    public const string QueueName = "reset-tests";
    public const string BaseItemName = "base";
    public const string MessageText = "queued";
    public const int ReferenceItemCount = 2;
    public const int QueuedMessageCount = 3;

    public static readonly string[] ReferenceItemNames = ["unit-a", "unit-b"];

    public static TheoryData<string> RecordNames => ["first", "second"];
}
