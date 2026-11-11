namespace HealthyFac.Insights.Application.Extraction;

/// <summary>
/// Reads the metric keys and the status codes that a machine already has in Telemetry, so the extraction can tell a
/// new definition from an existing one.
/// </summary>
public interface IMachineSignalCatalog
{
    /// <summary>
    /// Reads the signals of a machine as the service, for the actor of the current flow.
    /// </summary>
    /// <param name="machineId">The machine.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>
    /// The active metric keys and all status codes. Both lists are empty when Telemetry has no such machine.
    /// </returns>
    Task<MachineSignals> GetAsync(Guid machineId, CancellationToken cancellationToken);
}

/// <summary>
/// The metric keys and the status codes of a machine in Telemetry.
/// </summary>
public record MachineSignals
{
    public required IReadOnlyList<string> MetricKeys { get; init; }

    public required IReadOnlyList<string> StatusCodes { get; init; }
}
