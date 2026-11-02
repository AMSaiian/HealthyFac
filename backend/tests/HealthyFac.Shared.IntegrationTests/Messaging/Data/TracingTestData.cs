namespace HealthyFac.Shared.IntegrationTests.Messaging.Data;

public static class TracingTestData
{
    public const string NoParentText = "tracing without parent";
    public const string LinkedText = "tracing with parent";
    public const string ParentName = "test-parent";
    public const int ExpectedSpanCount = 3;

    public static readonly Guid NoParentRowId = Guid.Parse("019a0000-0000-7000-8000-000000000301");
    public static readonly Guid FailingRowId = Guid.Parse("019a0000-0000-7000-8000-000000000302");
}
