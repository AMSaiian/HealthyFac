using HealthyFac.Core.Application;
using HealthyFac.Telemetry.Application;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.MachineSimulator.Machines;

/// <summary>
/// Reads the machines of the Core database with their registrations and definitions of the Telemetry database. Both
/// connections are read-only. The soft delete filters of the two contexts apply, so a deleted machine, registration,
/// or definition never appears.
/// </summary>
/// <param name="coreDbContext">Reads the machines, the companies, and the state history.</param>
/// <param name="telemetryDbContext">Reads the registrations and the definitions.</param>
/// <param name="logger">Reports a company with an unknown time zone.</param>
public sealed class MachineSnapshotReader(
    ICoreDbContext coreDbContext,
    ITelemetryDbContext telemetryDbContext,
    ILogger<MachineSnapshotReader> logger
)
{
    private readonly ICoreDbContext _coreDbContext = coreDbContext;
    private readonly ITelemetryDbContext _telemetryDbContext = telemetryDbContext;
    private readonly ILogger<MachineSnapshotReader> _logger = logger;

    /// <summary>
    /// Reads a snapshot of every machine that is not deleted. A company with an unknown time zone fails alone: its
    /// machines are left out and one error is logged.
    /// </summary>
    /// <param name="cancellationToken">Cancels the queries.</param>
    /// <returns>The snapshots of all machines.</returns>
    public async Task<IReadOnlyList<MachineSnapshot>> ReadAllAsync(CancellationToken cancellationToken)
    {
        var machines = await _coreDbContext.Machines
            .Select(machine => new
            {
                machine.Id,
                machine.CompanyId,
                machine.Name,
                machine.SerialNumber,
                machine.GatewayId,
                machine.ExpectedGoLiveOn,
            })
            .ToListAsync(cancellationToken);
        var zones = await ReadZonesAsync(cancellationToken);
        var history = (await _coreDbContext.MachineStateHistories
                .Select(row => new { row.MachineId, row.State, row.StartedAt })
                .ToListAsync(cancellationToken))
            .ToLookup(
                row => row.MachineId, row => new StateHistoryEntry { State = row.State, StartedAt = row.StartedAt });
        var registered = (await _telemetryDbContext.MachineRegistrations
                .Select(registration => registration.Id)
                .ToListAsync(cancellationToken))
            .ToHashSet();
        var definitions = await ReadDefinitionsAsync(cancellationToken);

        return machines
            .Where(machine => zones.ContainsKey(machine.CompanyId))
            .Select(machine => new MachineSnapshot
            {
                MachineId = machine.Id, CompanyId = machine.CompanyId, Name = machine.Name,
                SerialNumber = machine.SerialNumber, GatewayId = machine.GatewayId,
                ExpectedGoLiveOn = machine.ExpectedGoLiveOn, Zone = zones[machine.CompanyId],
                StateHistory = history[machine.Id].ToList(), IsRegistered = registered.Contains(machine.Id),
                Definitions = definitions.GetValueOrDefault(machine.Id) ?? MachineDefinitions.Empty,
            })
            .ToList();
    }

    private async Task<Dictionary<Guid, TimeZoneInfo>> ReadZonesAsync(CancellationToken cancellationToken)
    {
        var companies = await _coreDbContext.Companies
            .Select(company => new { company.Id, company.TimeZoneId })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, TimeZoneInfo> zones = [];

        foreach (var company in companies)
        {
            try
            {
                zones[company.Id] = TimeZoneInfo.FindSystemTimeZoneById(company.TimeZoneId);
            }
            catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                _logger.LogError(exception, message: "The company {CompanyId} has an unknown time zone", company.Id);
            }
        }

        return zones;
    }

    private async Task<Dictionary<Guid, MachineDefinitions>> ReadDefinitionsAsync(CancellationToken cancellationToken)
    {
        var metrics = (await _telemetryDbContext.MetricDefinitions.ToListAsync(cancellationToken))
            .ToLookup(
                metric => metric.MachineRegistrationId,
                metric => new MetricInfo
                {
                    Key = metric.Key, ValueKind = metric.ValueKind, Min = metric.Min, Max = metric.Max,
                    States = metric.States,
                });
        var statuses = (await _telemetryDbContext.StatusDefinitions
                .Select(status => new { status.MachineRegistrationId, status.Code })
                .ToListAsync(cancellationToken))
            .ToLookup(status => status.MachineRegistrationId, status => status.Code);

        return metrics.Select(group => group.Key)
            .Union(statuses.Select(group => group.Key))
            .ToDictionary(
                registrationId => registrationId,
                registrationId => new MachineDefinitions
                    { Metrics = metrics[registrationId].ToList(), StatusCodes = statuses[registrationId].ToList() });
    }
}
