using HealthyFac.Insights.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Insights.ArchitectureTests.Conventions.Data;

public static class SoftDeleteTestData
{
    /// <summary>
    /// The entities that are not soft deletable on purpose: the ingested document and its chunks. A delete of the
    /// file removes them, and the cascade removes the chunks. A new exempt entity needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> ExemptEntityNames =
    [
        "IngestedDocument",
        "DocumentChunk",
    ];

    public static TheoryData<string> EntityAssemblies =>
    [
        LayerDependencyTestData.InsightsDomain,
    ];
}
