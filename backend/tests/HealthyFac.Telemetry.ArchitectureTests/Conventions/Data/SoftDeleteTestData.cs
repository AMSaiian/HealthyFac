using HealthyFac.Telemetry.ArchitectureTests.Dependencies.Data;

namespace HealthyFac.Telemetry.ArchitectureTests.Conventions.Data;

public static class SoftDeleteTestData
{
    /// <summary>
    /// The entities that are not soft deletable on purpose: the append-only entities, and the evaluation data that
    /// the detection owns and replaces (the statistics, the streaks, the machine evaluation, and the signal episodes,
    /// which are kept as history), and the phase intervals (append-only: only the end of the open
    /// interval changes). A new exempt entity needs a deliberate entry here.
    /// </summary>
    public static readonly IReadOnlyList<string> ExemptEntityNames =
    [
        "ReceivedMessage",
        "MetricSchemaRevision",
        "MachineEvaluation",
        "MetricStatistics",
        "RuleStreak",
        "SignalEpisode",
        "PhaseInterval",
    ];

    public static TheoryData<string> EntityAssemblies =>
    [
        LayerDependencyTestData.TelemetryDomain,
    ];
}
