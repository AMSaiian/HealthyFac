using HealthyFac.MachineSimulator.Scenarios.Machines;
using HealthyFac.Telemetry.Application.Intake;

namespace HealthyFac.Seeder.TelemetryHistory.Persistence;

/// <summary>What the history needs to know about one machine in Telemetry to build its messages.</summary>
public sealed class HistoryMachineData
{
    public required Guid RegistrationId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string GatewayId { get; init; }

    public required DateOnly ExpectedGoLiveOn { get; init; }

    /// <summary>The number of the latest schema revision.</summary>
    public required int SchemaRevision { get; init; }

    /// <summary>The JSON of the latest schema revision.</summary>
    public required string SchemaJson { get; init; }

    public required IReadOnlyDictionary<string, Guid> MetricIdsByKey { get; init; }

    public required IReadOnlyList<StatusDefinitionKey> StatusDefinitions { get; init; }

    public required MachineDefinitions Definitions { get; init; }
}
