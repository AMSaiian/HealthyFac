using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.MachineSimulator.Scenarios;

namespace HealthyFac.Seeder.TelemetryHistory;

/// <summary>A machine of <c>machines.json</c> with its draft and its scenario.</summary>
internal sealed class HistoryMachine
{
    public required string Key { get; init; }

    /// <summary>The index of the machine in <c>machines.json</c>. It adds to the random seed.</summary>
    public required int Index { get; init; }

    public required ProposedMachine Draft { get; init; }

    public required IMachineScenario Scenario { get; init; }
}
