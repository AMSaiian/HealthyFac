using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.SignalEpisodes;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record GetPhaseTimesRequest : IRequest<PhaseTimesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}

/// <summary>
/// The time of a machine in each phase over a range, in seconds. The phase seconds, the unknown mode seconds, and the
/// no-data seconds add up to the length of the range.
/// </summary>
public record PhaseTimesResponse
{
    public required IReadOnlyList<PhaseTimeResponse> Phases { get; init; }

    /// <summary>The seconds in which no phase matched.</summary>
    public required double UnknownModeSeconds { get; init; }

    /// <summary>The seconds without data: the time that no phase interval covers.</summary>
    public required double NoDataSeconds { get; init; }
}

public record PhaseTimeResponse
{
    public required Guid PhaseId { get; init; }

    public required string Code { get; init; }

    public required string Name { get; init; }

    public required bool IsTracked { get; init; }

    /// <summary>A deleted phase keeps its time and its name.</summary>
    public required bool IsDeleted { get; init; }

    public required double Seconds { get; init; }
}

public class GetPhaseTimesRequestValidator : AbstractValidator<GetPhaseTimesRequest>
{
    public GetPhaseTimesRequestValidator()
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
public sealed class GetPhaseTimesPreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetPhaseTimesRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetPhaseTimesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Reads the time of a machine in each phase over a range. Each phase interval is clipped to the range; an open
/// interval runs up to the current time. The answer holds every phase of the machine (a phase without time has 0
/// seconds) and every deleted phase that has time, then the unknown mode seconds and the seconds without data.
/// </summary>
/// <param name="dbContext">Reads the intervals and the phases.</param>
/// <param name="timeProvider">Provides the current time for an open interval.</param>
public sealed class GetPhaseTimesRequestHandler(ITelemetryDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<GetPhaseTimesRequest, PhaseTimesResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<PhaseTimesResponse> HandleAsync(
        GetPhaseTimesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var from = request.From;
        var to = request.To;
        var spans = await _dbContext.PhaseIntervals
            .AsNoTracking()
            .Where(interval => interval.MachineRegistrationId == request.MachineId
                && interval.StartedAt < to
                && (interval.EndedAt == null || interval.EndedAt > from))
            .Select(interval => new PhaseIntervalSpan
            {
                PhaseId = interval.OperatingPhaseId,
                StartedAt = interval.StartedAt,
                EndedAt = interval.EndedAt,
            })
            .ToListAsync(cancellationToken);
        var times = PhaseTimeCalculator.Calculate(spans, from, to, _timeProvider.GetUtcNow().UtcDateTime);
        var timedPhaseIds = times.PhaseSeconds.Keys.ToList();
        var phases = await _dbContext.OperatingPhases
            .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .AsNoTracking()
            .Where(phase => phase.MachineRegistrationId == request.MachineId
                && (phase.DeletedAt == null || timedPhaseIds.Contains(phase.Id)))
            .OrderBy(phase => phase.Priority)
            .Select(phase => new PhaseTimeResponse
            {
                PhaseId = phase.Id,
                Code = phase.Code,
                Name = phase.Name,
                IsTracked = phase.IsTracked,
                IsDeleted = phase.DeletedAt != null,
                Seconds = 0,
            })
            .ToListAsync(cancellationToken);

        return new PhaseTimesResponse
        {
            Phases = phases
                .Select(phase => phase with { Seconds = times.PhaseSeconds.GetValueOrDefault(phase.PhaseId) })
                .ToList(),
            UnknownModeSeconds = times.UnknownModeSeconds,
            NoDataSeconds = times.NoDataSeconds,
        };
    }
}
