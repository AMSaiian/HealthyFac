using HealthyFac.Telemetry.Application;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.MachineSimulator.Machines.Snapshots;

/// <summary>
/// Reads the latest stored value of the counters of a machine, so a counter continues after a restart of the simulator.
/// </summary>
/// <param name="telemetryDbContext">Reads the metric definitions and the numeric readings.</param>
public sealed class CounterReadingReader(ITelemetryDbContext telemetryDbContext)
{
    private readonly ITelemetryDbContext _telemetryDbContext = telemetryDbContext;

    /// <summary>
    /// Reads the latest reading of each counter key that has a definition.
    /// </summary>
    /// <param name="machineId">The machine ID, which is the registration ID in Telemetry.</param>
    /// <param name="counterKeys">The metric keys of the counters.</param>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The latest value by key. A key without a reading has no entry.</returns>
    public async Task<IReadOnlyDictionary<string, double>> ReadLatestAsync(
        Guid machineId,
        IReadOnlyCollection<string> counterKeys,
        CancellationToken cancellationToken
    )
    {
        var keys = counterKeys.ToArray();

        var definitions = await _telemetryDbContext.MetricDefinitions
            .Where(definition => definition.MachineRegistrationId == machineId && keys.Contains(definition.Key))
            .Select(definition => new { definition.Id, definition.Key })
            .ToListAsync(cancellationToken);

        Dictionary<string, double> latest = [];

        foreach (var definition in definitions)
        {
            var value = await _telemetryDbContext.NumericReadings
                .Where(reading => reading.MetricDefinitionId == definition.Id)
                .OrderByDescending(reading => reading.SampledAt)
                .Select(reading => (double?)reading.Value)
                .FirstOrDefaultAsync(cancellationToken);

            if (value is { } stored)
            {
                latest[definition.Key] = stored;
            }
        }

        return latest;
    }
}
