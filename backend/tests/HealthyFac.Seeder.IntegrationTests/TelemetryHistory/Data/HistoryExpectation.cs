namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

/// <summary>The moments of one telemetry script, as the checks of the history read them.</summary>
public sealed class HistoryExpectation
{
    public required int WindowHours { get; init; }

    public required string LatheDrainFrom { get; init; }

    public required string LatheMaintenanceFrom { get; init; }

    public required string LatheMaintenanceUntil { get; init; }

    public required string PumpDrainFrom { get; init; }

    public required string PumpMaintenanceFrom { get; init; }

    public required string PumpMaintenanceUntil { get; init; }

    public required string CompressorStepFrom { get; init; }

    public required string CompressorStepUntil { get; init; }

    public required string PumpGapFrom { get; init; }

    public required string PumpGapUntil { get; init; }

    public required string LatheOverrideFrom { get; init; }

    public required string LatheOverrideUntil { get; init; }

    /// <summary>The samples of one machine without a gap: one each second, the window start and the anchor included.
    /// </summary>
    public int SampleCount => WindowHours * 3600 + 1;

    /// <summary>The samples that the gap of the pump removes.</summary>
    public required int GapSampleCount { get; init; }
}
