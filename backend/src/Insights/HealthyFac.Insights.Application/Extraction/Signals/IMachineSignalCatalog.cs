namespace HealthyFac.Insights.Application.Extraction.Signals;

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
    /// The active metric definitions and all status definitions. Both lists are empty when Telemetry has no such
    /// machine.
    /// </returns>
    Task<MachineSignals> GetAsync(Guid machineId, CancellationToken cancellationToken);
}
