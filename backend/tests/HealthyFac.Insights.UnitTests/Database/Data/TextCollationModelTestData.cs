namespace HealthyFac.Insights.UnitTests.Database.Data;

public static class TextCollationModelTestData
{
    /// <summary>
    /// The text columns that compare case-sensitively on purpose, as <c>Entity.Property</c>. A chunk is source
    /// text and not a name. A new case-sensitive column needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> CaseSensitiveColumns =
    [
        "DocumentChunk.Text",
    ];
}
