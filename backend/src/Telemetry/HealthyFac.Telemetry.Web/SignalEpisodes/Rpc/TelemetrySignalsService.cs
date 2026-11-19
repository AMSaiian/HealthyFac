using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application.SignalEpisodes;
using HealthyFac.Telemetry.Contracts.Signals;
using HealthyFac.Telemetry.Web.Authorization;
using Microsoft.AspNetCore.Authorization;
using ApplicationGetMetricHistoryRequest =
    HealthyFac.Telemetry.Application.SignalEpisodes.GetMetricHistoryRequest;
using ApplicationGetSignalEpisodesRequest =
    HealthyFac.Telemetry.Application.SignalEpisodes.GetSignalEpisodesRequest;
using RpcGetMetricHistoryRequest = HealthyFac.Telemetry.Contracts.Signals.GetMetricHistoryRequest;
using RpcGetSignalEpisodesRequest = HealthyFac.Telemetry.Contracts.Signals.GetSignalEpisodesRequest;

namespace HealthyFac.Telemetry.Web.SignalEpisodes.Rpc;

/// <summary>
/// Serves the metric history and the signal episodes to the services that read them (Insights) on the gRPC port. The
/// caller sends the plain GUIDs as text. A user token or a service token with actor headers works; the actor company
/// scopes every read.
/// </summary>
/// <param name="historyHandler">Reads the metric history.</param>
/// <param name="episodesHandler">Reads the signal episodes.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
[Authorize(Policy = TelemetryPermissions.Signals.Read)]
public sealed class TelemetrySignalsService(
    IRequestHandler<ApplicationGetMetricHistoryRequest, MetricHistoryResponse> historyHandler,
    IRequestHandler<ApplicationGetSignalEpisodesRequest, SignalEpisodesResponse> episodesHandler,
    ICurrentUserProvider currentUser
) : TelemetrySignals.TelemetrySignalsBase
{
    private readonly IRequestHandler<ApplicationGetMetricHistoryRequest, MetricHistoryResponse> _historyHandler =
        historyHandler;

    private readonly IRequestHandler<ApplicationGetSignalEpisodesRequest, SignalEpisodesResponse> _episodesHandler =
        episodesHandler;

    private readonly ICurrentUserProvider _currentUser = currentUser;

    public override async Task<MetricHistoryReply> GetMetricHistory(
        RpcGetMetricHistoryRequest request,
        ServerCallContext context
    )
    {
        var response = await _historyHandler.HandleAsync(
            new ApplicationGetMetricHistoryRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                MachineId = ParseId(request.MachineId),
                MetricDefinitionId = ParseId(request.MetricDefinitionId),
                From = request.From.ToDateTime(),
                To = request.To.ToDateTime(),
                BucketCount = request.BucketCount,
            },
            context.CancellationToken);

        var reply = new MetricHistoryReply();
        reply.Buckets.AddRange(response.Buckets.Select(ToBucket));

        return reply;
    }

    public override async Task<SignalEpisodesReply> GetSignalEpisodes(
        RpcGetSignalEpisodesRequest request,
        ServerCallContext context
    )
    {
        var response = await _episodesHandler.HandleAsync(
            new ApplicationGetSignalEpisodesRequest
            {
                CompanyId = _currentUser.GetCompanyId(),
                MachineId = ParseId(request.MachineId),
                From = request.From.ToDateTime(),
                To = request.To.ToDateTime(),
            },
            context.CancellationToken);

        var reply = new SignalEpisodesReply();
        reply.Episodes.AddRange(response.Items.Select(ToEpisode));

        return reply;
    }

    private static Guid ParseId(string id) =>
        Guid.TryParse(id, out var parsed) ? parsed : throw new NotFoundException(RpcErrors.IdInvalid);

    private static MetricBucket ToBucket(MetricBucketResponse bucket) =>
        new()
        {
            Start = ToTimestamp(bucket.Start),
            Avg = bucket.Avg,
            Min = bucket.Min,
            Max = bucket.Max,
            Count = bucket.Count,
        };

    private static SignalEpisode ToEpisode(SignalEpisodeResponse episode)
    {
        var message = new SignalEpisode
        {
            EpisodeId = episode.EpisodeId.ToString(),
            MachineId = episode.MachineId.ToString(),
            CompanyId = episode.CompanyId.ToString(),
            Rule = episode.Rule,
            Severity = episode.Severity,
            State = episode.State,
            OpenedAt = ToTimestamp(episode.OpenedAt),
            Sequence = episode.Sequence,
        };

        if (episode.MetricDefinitionId is { } metricId)
        {
            message.MetricDefinitionId = metricId.ToString();
        }

        if (episode.StatusDefinitionId is { } statusId)
        {
            message.StatusDefinitionId = statusId.ToString();
        }

        if (episode.MetricKey is { } metricKey)
        {
            message.MetricKey = metricKey;
        }

        if (episode.StatusCode is { } statusCode)
        {
            message.StatusCode = statusCode;
        }

        if (episode.PhaseCode is { } phaseCode)
        {
            message.PhaseCode = phaseCode;
        }

        if (episode.Value is { } value)
        {
            message.Value = value;
        }

        if (episode.Limit is { } limit)
        {
            message.Limit = limit;
        }

        if (episode.Z is { } z)
        {
            message.Z = z;
        }

        if (episode.ForecastSeconds is { } forecastSeconds)
        {
            message.ForecastSeconds = forecastSeconds;
        }

        if (episode.ResolvedAt is { } resolvedAt)
        {
            message.ResolvedAt = ToTimestamp(resolvedAt);
        }

        return message;
    }

    private static Timestamp ToTimestamp(DateTime value) =>
        Timestamp.FromDateTime(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
