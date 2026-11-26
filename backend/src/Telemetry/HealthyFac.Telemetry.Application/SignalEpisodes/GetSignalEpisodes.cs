using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.SignalEpisodes;

public record GetSignalEpisodesRequest : IRequest<SignalEpisodesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}

public record SignalEpisodesResponse
{
    public required IReadOnlyList<SignalEpisodeResponse> Items { get; init; }
}

/// <summary>
/// One signal episode with its latest snapshot. The fields are the fields of the <c>SignalEpisodeChanged</c> message
/// without its change time.
/// </summary>
public record SignalEpisodeResponse
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

public class GetSignalEpisodesRequestValidator : AbstractValidator<GetSignalEpisodesRequest>
{
    public GetSignalEpisodesRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(SignalReadErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(SignalReadErrors.MachineIdRequired);

        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .WithMessage(SignalReadErrors.RangeInvalid);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a deleted machine, are "not found"
/// (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetSignalEpisodesPreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetSignalEpisodesRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetSignalEpisodesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Lists the episodes of a machine that overlap a range: an episode that opened before the end of the range and is
/// still open or resolved after its start. The list is in the order of the open time.
/// </summary>
/// <remarks>
/// The metric key and the status code come from the definitions of the machine, also from deleted ones, because an
/// episode outlives its definition. The phase code is the code that the episode stored with its snapshot.
/// </remarks>
/// <param name="dbContext">Reads the episodes and the definitions.</param>
public sealed class GetSignalEpisodesRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetSignalEpisodesRequest, SignalEpisodesResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<SignalEpisodesResponse> HandleAsync(
        GetSignalEpisodesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var episodes = await _dbContext.SignalEpisodes
            .AsNoTracking()
            .Where(episode => episode.MachineRegistrationId == request.MachineId
                && episode.OpenedAt < request.To
                && (episode.ResolvedAt == null || episode.ResolvedAt > request.From))
            .OrderBy(episode => episode.OpenedAt)
            .ThenBy(episode => episode.Id)
            .ToListAsync(cancellationToken);

        var metricKeys = await _dbContext.MetricDefinitions
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .Where(metric => metric.MachineRegistrationId == request.MachineId)
            .ToDictionaryAsync(metric => metric.Id, metric => metric.Key, cancellationToken);

        var statusCodes = await _dbContext.StatusDefinitions
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .Where(status => status.MachineRegistrationId == request.MachineId)
            .ToDictionaryAsync(status => status.Id, status => status.Code, cancellationToken);

        return new SignalEpisodesResponse
        {
            Items = [.. episodes.Select(episode => ToResponse(episode, metricKeys, statusCodes))],
        };
    }

    private static SignalEpisodeResponse ToResponse(
        SignalEpisode episode,
        IReadOnlyDictionary<Guid, string> metricKeys,
        IReadOnlyDictionary<Guid, string> statusCodes
    ) =>
        new()
        {
            EpisodeId = episode.Id,
            MachineId = episode.MachineRegistrationId,
            CompanyId = episode.CompanyId,
            Rule = episode.Rule.Value,
            Severity = episode.Severity.Value,
            State = episode.State.Value,
            MetricDefinitionId = episode.MetricDefinitionId,
            MetricKey = episode.MetricDefinitionId is { } metricId && metricKeys.TryGetValue(metricId, out var key)
                ? key
                : null,
            StatusDefinitionId = episode.StatusDefinitionId,
            StatusCode = episode.StatusDefinitionId is { } statusId && statusCodes.TryGetValue(statusId, out var code)
                ? code
                : null,
            PhaseCode = episode.Snapshot.PhaseCode,
            Value = episode.Snapshot.Value,
            Limit = episode.Snapshot.Limit,
            Z = episode.Snapshot.Z,
            ForecastSeconds = episode.Snapshot.ForecastSeconds,
            OpenedAt = episode.OpenedAt,
            ResolvedAt = episode.ResolvedAt,
            Sequence = episode.Sequence,
        };
}
