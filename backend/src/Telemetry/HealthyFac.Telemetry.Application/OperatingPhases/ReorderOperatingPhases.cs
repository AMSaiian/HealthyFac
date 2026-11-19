using EntityFramework.Exceptions.Common;
using FluentValidation;
using FluentValidation.Results;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record ReorderOperatingPhasesRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required IReadOnlyList<Guid> PhaseIds { get; init; }
}

public class ReorderOperatingPhasesRequestValidator : AbstractValidator<ReorderOperatingPhasesRequest>
{
    public ReorderOperatingPhasesRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.MachineIdRequired);

        RuleFor(x => x.PhaseIds)
            .NotNull();
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a deleted machine, are "not found"
/// (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ReorderOperatingPhasesPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<ReorderOperatingPhasesRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ReorderOperatingPhasesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Sets the priorities of all phases of a machine to 0, 1, 2, and so on, in the order of the request. The request
/// must hold the ID of each active phase of the machine exactly once.
/// </summary>
/// <remarks>
/// The unique index of the priority is checked after each statement, so a direct swap would collide. The handler
/// saves twice in one transaction: the first save moves every phase above the highest priority, and the second save
/// sets the final priorities. The operation runs again after a transient failure and gives the same result.
/// </remarks>
/// <param name="dbContext">Loads the phases and saves the priorities in one transaction.</param>
public sealed class ReorderOperatingPhasesRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<ReorderOperatingPhasesRequest>
{
    private static readonly string PhaseIdsKey = ValidationNames.GetJsonName(
        typeof(ReorderOperatingPhasesRequest).GetProperty(nameof(ReorderOperatingPhasesRequest.PhaseIds))!);

    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        ReorderOperatingPhasesRequest request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _dbContext.RunInTransactionAsync(
                token => ReorderAsync(request, token),
                cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(OperatingPhaseErrors.CodeOrPriorityTaken, exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(OperatingPhaseErrors.PhaseChanged, exception);
        }
    }

    private async Task ReorderAsync(ReorderOperatingPhasesRequest request, CancellationToken cancellationToken)
    {
        var phases = await _dbContext.OperatingPhases
            .Where(phase => phase.MachineRegistrationId == request.MachineId)
            .OrderBy(phase => phase.Id)
            .ToListAsync(cancellationToken);

        var isOrderComplete = request.PhaseIds.Distinct().Count() == request.PhaseIds.Count
            && request.PhaseIds.Count == phases.Count
            && phases.All(phase => request.PhaseIds.Contains(phase.Id));

        if (!isOrderComplete)
        {
            throw new AppValidationException(
                [new ValidationFailure(PhaseIdsKey, OperatingPhaseErrors.PhaseIdsInvalid)]);
        }

        var highestPriority = phases.Count == 0 ? 0 : phases.Max(phase => phase.Priority);

        for (var index = 0; index < request.PhaseIds.Count; index++)
        {
            phases.Single(phase => phase.Id == request.PhaseIds[index]).ChangePriority(highestPriority + 1 + index);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        for (var index = 0; index < request.PhaseIds.Count; index++)
        {
            phases.Single(phase => phase.Id == request.PhaseIds[index]).ChangePriority(index);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
