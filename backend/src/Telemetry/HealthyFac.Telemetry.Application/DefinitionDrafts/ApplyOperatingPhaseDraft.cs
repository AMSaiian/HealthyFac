using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.OperatingPhases;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.DefinitionDrafts;

/// <summary>
/// Turns a phase draft into a phase. The body holds the values that the user reviewed, as the create call does.
/// </summary>
public record ApplyOperatingPhaseDraftRequest : OperatingPhaseInput, IRequest<ApplyOperatingPhaseDraftResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    /// <summary>The ID of the phase draft.</summary>
    public required Guid Id { get; init; }
}

public record ApplyOperatingPhaseDraftResponse
{
    public required Guid Id { get; init; }
}

public class ApplyOperatingPhaseDraftRequestValidator : AbstractValidator<ApplyOperatingPhaseDraftRequest>
{
    public ApplyOperatingPhaseDraftRequestValidator(IRichTextPolicy richTextPolicy)
    {
        this.AddOperatingPhaseInputRules(richTextPolicy);

        RuleFor(x => x.CompanyId).NotEmpty().WithMessage(DefinitionDraftErrors.CompanyIdRequired);
        RuleFor(x => x.MachineId).NotEmpty().WithMessage(DefinitionDraftErrors.MachineIdRequired);
        RuleFor(x => x.Id).NotEmpty().WithMessage(DefinitionDraftErrors.DraftIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a deleted machine, are "not found"
/// (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class ApplyOperatingPhaseDraftPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<ApplyOperatingPhaseDraftRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ApplyOperatingPhaseDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Creates the phase of a pending phase draft and marks the draft applied in one save. A draft of another company or
/// machine, or an unknown ID, is "not found" (<c>404</c>). A draft that is not pending is "unprocessable" (<c>422</c>,
/// the key <c>draftNotPending</c>). A draft whose condition or limit row still names a metric or a status draft is
/// "unprocessable" too (the key <c>draftUsesDrafts</c>): the user applies those drafts first, and the apply moves the
/// rows to the new definitions. The phase follows the create rules (<see cref="OperatingPhaseBodyBuilder"/>).
/// </summary>
/// <remarks>
/// The handler never queries for an existing code or priority. The two unique indexes of the active phases of the
/// machine are the only guards, and the handler maps their failure to the conflict.
/// </remarks>
/// <param name="dbContext">Loads the draft, reads the definitions, and saves the phase.</param>
public sealed class ApplyOperatingPhaseDraftRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<ApplyOperatingPhaseDraftRequest, ApplyOperatingPhaseDraftResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<ApplyOperatingPhaseDraftResponse> HandleAsync(
        ApplyOperatingPhaseDraftRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var draft = await _dbContext.OperatingPhaseDrafts
                .Include(candidate => candidate.Conditions)
                .Include(candidate => candidate.Limits)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id
                        && candidate.CompanyId == request.CompanyId
                        && candidate.TargetMachineId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(DefinitionDraftErrors.DraftNotFound);

        if (draft.State != DefinitionDraftState.Pending)
        {
            throw new UnprocessableException(DefinitionDraftErrors.DraftNotPending);
        }

        if (draft.HasUnresolvedTargets)
        {
            throw new UnprocessableException(DefinitionDraftErrors.DraftUsesDrafts);
        }

        var body = await _dbContext.BuildPhaseBodyAsync(request, request.MachineId, cancellationToken);
        var phase = new OperatingPhase(
            request.MachineId,
            request.Code,
            request.Name,
            request.Description,
            request.Priority,
            request.IsTracked,
            body.Conditions,
            body.Limits);
        _dbContext.OperatingPhases.Add(phase);
        draft.MarkApplied();

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(OperatingPhaseErrors.CodeOrPriorityTaken, exception);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(DefinitionDraftErrors.DraftChanged, exception);
        }

        return new ApplyOperatingPhaseDraftResponse { Id = phase.Id };
    }
}
