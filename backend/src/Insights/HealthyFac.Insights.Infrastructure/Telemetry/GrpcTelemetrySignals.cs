using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Application.Signals;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Telemetry.Contracts.Signals;

namespace HealthyFac.Insights.Infrastructure.Telemetry;

/// <summary>
/// Reads the metric history and the signal episodes from Telemetry through its gRPC port. The call uses the service
/// token with the actor headers, and enters the downstream scope of the call for the call only. A <c>NotFound</c> reply
/// (an unknown machine or metric, or another company) gives an empty result, so the caller never learns which case
/// it was.
/// </summary>
/// <param name="clientFactory">Creates the named client.</param>
/// <param name="downstreamScopes">Holds the downstream scope of the call.</param>
public sealed class GrpcTelemetrySignals(GrpcClientFactory clientFactory, IDownstreamScopeHolder downstreamScopes)
    : ITelemetrySignals
{
    private readonly GrpcClientFactory _clientFactory = clientFactory;
    private const string UnavailableMessage = "Telemetry did not answer the signals request.";

    private readonly IDownstreamScopeHolder _downstreamScopes = downstreamScopes;

    public async Task<IReadOnlyList<MetricHistoryBucket>> GetMetricHistoryAsync(
        Guid machineId,
        Guid metricId,
        DateTime from,
        DateTime to,
        int bucketCount,
        CancellationToken cancellationToken
    )
    {
        var client = _clientFactory.CreateClient<TelemetrySignals.TelemetrySignalsClient>(TelemetryClientNames.Signals);

        using var lease = _downstreamScopes.Enter(TelemetryClientNames.DownstreamScope);

        try
        {
            var reply = await client.GetMetricHistoryAsync(
                new GetMetricHistoryRequest
                {
                    MachineId = machineId.ToString(),
                    MetricDefinitionId = metricId.ToString(),
                    From = ToTimestamp(from),
                    To = ToTimestamp(to),
                    BucketCount = bucketCount,
                },
                cancellationToken: cancellationToken);

            return [.. reply.Buckets.Select(ToBucket)];
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return [];
        }
        catch (RpcException exception) when (IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }

    public async Task<IReadOnlyList<SignalEpisodeInfo>> GetSignalEpisodesAsync(
        Guid machineId,
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken
    )
    {
        var client = _clientFactory.CreateClient<TelemetrySignals.TelemetrySignalsClient>(TelemetryClientNames.Signals);

        using var lease = _downstreamScopes.Enter(TelemetryClientNames.DownstreamScope);

        try
        {
            var reply = await client.GetSignalEpisodesAsync(
                new GetSignalEpisodesRequest
                {
                    MachineId = machineId.ToString(),
                    From = ToTimestamp(from),
                    To = ToTimestamp(to),
                },
                cancellationToken: cancellationToken);

            return [.. reply.Episodes.Select(ToEpisode)];
        }
        catch (RpcException exception) when (exception.StatusCode == StatusCode.NotFound)
        {
            return [];
        }
        catch (RpcException exception) when (IsOutage(exception))
        {
            throw new ProviderUnavailableException(UnavailableMessage, exception);
        }
    }

    /// <summary>
    /// Tells whether the call failed because Telemetry did not answer (the status <c>Unavailable</c> or
    /// <c>DeadlineExceeded</c>). The caller maps it to a <see cref="ProviderUnavailableException"/>.
    /// </summary>
    /// <param name="exception">The gRPC failure.</param>
    /// <returns><see langword="true"/> for an outage.</returns>
    internal static bool IsOutage(RpcException exception) =>
        exception.StatusCode is StatusCode.Unavailable or StatusCode.DeadlineExceeded;

    private static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));

    private static MetricHistoryBucket ToBucket(MetricBucket bucket) =>
        new()
        {
            Start = bucket.Start.ToDateTime(),
            Avg = bucket.Avg,
            Min = bucket.Min,
            Max = bucket.Max,
            Count = bucket.Count,
        };

    private static SignalEpisodeInfo ToEpisode(SignalEpisode episode) =>
        new()
        {
            EpisodeId = Guid.Parse(episode.EpisodeId),
            MachineId = Guid.Parse(episode.MachineId),
            CompanyId = Guid.Parse(episode.CompanyId),
            Rule = episode.Rule,
            Severity = episode.Severity,
            State = episode.State,
            MetricDefinitionId = episode.HasMetricDefinitionId ? Guid.Parse(episode.MetricDefinitionId) : null,
            MetricKey = episode.HasMetricKey ? episode.MetricKey : null,
            StatusDefinitionId = episode.HasStatusDefinitionId ? Guid.Parse(episode.StatusDefinitionId) : null,
            StatusCode = episode.HasStatusCode ? episode.StatusCode : null,
            PhaseCode = episode.HasPhaseCode ? episode.PhaseCode : null,
            Value = episode.HasValue ? episode.Value : null,
            Limit = episode.HasLimit ? episode.Limit : null,
            Z = episode.HasZ ? episode.Z : null,
            ForecastSeconds = episode.HasForecastSeconds ? episode.ForecastSeconds : null,
            OpenedAt = episode.OpenedAt.ToDateTime(),
            ResolvedAt = episode.ResolvedAt?.ToDateTime(),
            Sequence = episode.Sequence,
        };
}
