namespace HealthyFac.Insights.Application.Signals;

/// <summary>
/// Reads the metric history and the signal episodes of a machine from the service that owns them (Telemetry). The
/// reads run as the service for the actor of the current flow, and Telemetry scopes each read to the company of the
/// actor.
/// </summary>
public interface ITelemetrySignals
{
    /// <summary>
    /// Reads the readings of a metric as buckets of equal width.
    /// </summary>
    /// <param name="machineId">The machine that owns the metric.</param>
    /// <param name="metricId">The metric to read.</param>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    /// <param name="bucketCount">The number of buckets that cover the range.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The non-empty buckets in time order. An unknown machine or metric gives an empty list.</returns>
    Task<IReadOnlyList<MetricHistoryBucket>> GetMetricHistoryAsync(
        Guid machineId,
        Guid metricId,
        DateTime from,
        DateTime to,
        int bucketCount,
        CancellationToken cancellationToken
    );

    /// <summary>
    /// Reads the episodes of a machine that overlap a range.
    /// </summary>
    /// <param name="machineId">The machine to read.</param>
    /// <param name="from">The start of the range.</param>
    /// <param name="to">The end of the range.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The episodes in the order of the open time. An unknown machine gives an empty list.</returns>
    Task<IReadOnlyList<SignalEpisodeInfo>> GetSignalEpisodesAsync(
        Guid machineId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    );
}

/// <summary>
/// One non-empty bucket of the readings of a metric.
/// </summary>
public sealed record MetricHistoryBucket
{
    public required DateTime Start { get; init; }

    public required double Avg { get; init; }

    public required double Min { get; init; }

    public required double Max { get; init; }

    public required long Count { get; init; }
}

/// <summary>
/// One signal episode with its latest snapshot. The rule, the severity, and the state keep the text values of the
/// <c>SignalEpisodeChanged</c> message.
/// </summary>
public sealed record SignalEpisodeInfo
{
    public required Guid EpisodeId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }

    public required string Rule { get; init; }

    public required string Severity { get; init; }

    public required string State { get; init; }

    public Guid? MetricDefinitionId { get; init; }

    public string? MetricKey { get; init; }

    public Guid? StatusDefinitionId { get; init; }

    public string? StatusCode { get; init; }

    public string? PhaseCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public double? Z { get; init; }

    public double? ForecastSeconds { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public required int Sequence { get; init; }
}
