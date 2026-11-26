using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.Evaluation;

/// <summary>
/// One sampling time of a machine with the values of that time.
/// </summary>
public sealed record Sample
{
    public required DateTime SampledAt { get; init; }

    public required SampleValues Values { get; init; }
}

/// <summary>
/// Reads the next batch of samples of a machine from the stored readings. A batch holds the sampling times after the
/// watermark and not after the settle limit, in order. The cap of readings per run limits the number of times.
/// </summary>
/// <param name="dbContext">Reads the received messages and the readings.</param>
public sealed class SampleBatchReader(ITelemetryDbContext dbContext)
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    /// <summary>
    /// Reads the batch.
    /// </summary>
    /// <param name="machineId">The machine.</param>
    /// <param name="watermark">The last evaluated sampling time, or <see langword="null"/> for none.</param>
    /// <param name="upTo">The latest sampling time that the batch may hold (now minus the settle lag).</param>
    /// <param name="maxSamples">The most sampling times that the batch may hold.</param>
    /// <param name="metrics">The metric definitions of the machine.</param>
    /// <param name="statusIds">The status definitions of the machine.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The samples in the order of their sampling time.</returns>
    public async Task<IReadOnlyList<Sample>> ReadAsync(
        Guid machineId,
        DateTime? watermark,
        DateTime upTo,
        int maxSamples,
        IReadOnlyCollection<MetricDefinition> metrics,
        IReadOnlyCollection<Guid> statusIds,
        CancellationToken cancellationToken
    )
    {
        var times = await ReadTimesAsync(machineId, watermark, upTo, maxSamples, cancellationToken);

        if (times.Count == 0)
        {
            return [];
        }

        var first = times[0];
        var last = times[^1];

        var numericIds = metrics.Where(metric => metric.ValueKind == MetricValueKind.Numeric)
            .Select(metric => metric.Id)
            .ToList();

        var discreteIds = metrics.Where(metric => metric.ValueKind != MetricValueKind.Numeric)
            .Select(metric => metric.Id)
            .ToList();

        var numeric = await _dbContext.NumericReadings
            .AsNoTracking()
            .Where(reading => numericIds.Contains(reading.MetricDefinitionId)
                && reading.SampledAt >= first
                && reading.SampledAt <= last)
            .ToListAsync(cancellationToken);

        var discrete = await _dbContext.DiscreteReadings
            .AsNoTracking()
            .Where(reading => discreteIds.Contains(reading.MetricDefinitionId)
                && reading.SampledAt >= first
                && reading.SampledAt <= last)
            .ToListAsync(cancellationToken);

        var statuses = await _dbContext.StatusReadings
            .AsNoTracking()
            .Where(reading => statusIds.Contains(reading.StatusDefinitionId)
                && reading.SampledAt >= first
                && reading.SampledAt <= last)
            .ToListAsync(cancellationToken);

        var numericByTime = numeric.ToLookup(reading => reading.SampledAt);
        var discreteByTime = discrete.ToLookup(reading => reading.SampledAt);
        var statusByTime = statuses.ToLookup(reading => reading.SampledAt);

        return
        [
            .. times
                .Select(time => new Sample
                {
                    SampledAt = time,
                    Values = new SampleValues
                    {
                        Numeric = numericByTime[time]
                            .ToDictionary(reading => reading.MetricDefinitionId, reading => reading.Value),
                        Discrete = discreteByTime[time]
                            .ToDictionary(reading => reading.MetricDefinitionId, reading => reading.Value),
                        ActiveStatusIds = statusByTime[time].Select(reading => reading.StatusDefinitionId).ToHashSet(),
                    },
                }),
        ];
    }

    /// <summary>
    /// Counts the received messages that arrived after the last run but carry a sampling time that the watermark has
    /// already passed. The evaluation never uses them.
    /// </summary>
    /// <param name="machineId">The machine.</param>
    /// <param name="lastRunAt">The time of the last run.</param>
    /// <param name="watermark">The last evaluated sampling time.</param>
    /// <param name="cancellationToken">Cancels the count.</param>
    /// <returns>The number of late messages.</returns>
    public Task<int> CountLateAsync(
        Guid machineId,
        DateTime lastRunAt,
        DateTime watermark,
        CancellationToken cancellationToken
    ) =>
        _dbContext.ReceivedMessages
            .AsNoTracking()
            .CountAsync(
                message => message.MachineRegistrationId == machineId
                    && message.ReceivedAt > lastRunAt
                    && message.SampledAt < watermark,
                cancellationToken);

    /// <summary>
    /// Finds the newest sampling time after the watermark that the evaluation may read.
    /// </summary>
    /// <param name="machineId">The machine.</param>
    /// <param name="watermark">The last evaluated sampling time, or <see langword="null"/> for none.</param>
    /// <param name="upTo">The latest sampling time that counts (now minus the settle lag).</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The time, or <see langword="null"/> when there is none.</returns>
    public Task<DateTime?> LatestSampledAtAsync(
        Guid machineId,
        DateTime? watermark,
        DateTime upTo,
        CancellationToken cancellationToken
    ) =>
        _dbContext.ReceivedMessages
            .AsNoTracking()
            .Where(message => message.MachineRegistrationId == machineId
                && message.SampledAt > (watermark ?? DateTime.MinValue)
                && message.SampledAt <= upTo)
            .MaxAsync(message => message.SampledAt, cancellationToken);

    /// <summary>
    /// Finds the latest receive time of the machine.
    /// </summary>
    /// <param name="machineId">The machine.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The time, or <see langword="null"/> when the machine never sent a message.</returns>
    public Task<DateTime?> LatestReceivedAtAsync(Guid machineId, CancellationToken cancellationToken) =>
        _dbContext.ReceivedMessages
            .AsNoTracking()
            .Where(message => message.MachineRegistrationId == machineId)
            .MaxAsync(message => (DateTime?)message.ReceivedAt, cancellationToken);

    /// <summary>
    /// Finds the first receive time of the machine after a time.
    /// </summary>
    /// <param name="machineId">The machine.</param>
    /// <param name="after">The time that the message must be later than.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The time, or <see langword="null"/> when no later message exists.</returns>
    public Task<DateTime?> FirstReceivedAfterAsync(
        Guid machineId,
        DateTime after,
        CancellationToken cancellationToken
    ) =>
        _dbContext.ReceivedMessages
            .AsNoTracking()
            .Where(message => message.MachineRegistrationId == machineId && message.ReceivedAt > after)
            .MinAsync(message => (DateTime?)message.ReceivedAt, cancellationToken);

    private async Task<List<DateTime>> ReadTimesAsync(
        Guid machineId,
        DateTime? watermark,
        DateTime upTo,
        int maxSamples,
        CancellationToken cancellationToken
    )
    {
        var query = _dbContext.ReceivedMessages
            .AsNoTracking()
            .Where(message => message.MachineRegistrationId == machineId
                && message.SampledAt != null
                && message.SampledAt <= upTo);

        if (watermark is { } last)
        {
            query = query.Where(message => message.SampledAt > last);
        }

        return await query
            .Select(message => message.SampledAt!.Value)
            .Distinct()
            .OrderBy(time => time)
            .Take(maxSamples)
            .ToListAsync(cancellationToken);
    }
}
