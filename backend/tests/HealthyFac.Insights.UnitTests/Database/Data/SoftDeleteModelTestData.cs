namespace HealthyFac.Insights.UnitTests.Database.Data;

public static class SoftDeleteModelTestData
{
    /// <summary>
    /// The unique indexes that keep deleted rows on purpose, as <c>Entity.index_name</c>. A new unfiltered unique
    /// index needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> UnfilteredUniqueIndexes =
        [];
}
