using HealthyFac.Insights.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Insights.ArchitectureTests.Conventions.Data;

public static class SoftDeleteTestData
{
    /// <summary>
    /// The entities that are not soft deletable on purpose: the ingested document and its chunks (a delete of the
    /// file removes them, and the cascade removes the chunks), and the cases, the signals, and the AI log rows
    /// (no user deletes them). A new exempt entity needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> ExemptEntityNames =
    [
        "IngestedDocument",
        "DocumentChunk",
        "MaintenanceCase",
        "MachineSignal",
        "AgentRun",
        "AgentToolCall",
        "AgentAction",
    ];

    public static TheoryData<string> EntityAssemblies =>
    [
        LayerDependencyTestData.InsightsDomain,
    ];
}
