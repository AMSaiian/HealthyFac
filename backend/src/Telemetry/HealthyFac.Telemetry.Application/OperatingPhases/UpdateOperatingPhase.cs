using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record UpdateOperatingPhaseRequest : OperatingPhaseInput, IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class UpdateOperatingPhaseRequestValidator : AbstractValidator<UpdateOperatingPhaseRequest>
{
    public UpdateOperatingPhaseRequestValidator(IRichTextPolicy richTextPolicy)
    {
        this.AddOperatingPhaseInputRules(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.MachineIdRequired);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.PhaseIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a deleted machine, are "not found"
/// (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class UpdateOperatingPhasePreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UpdateOperatingPhaseRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(UpdateOperatingPhaseRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Replaces the full body of a phase: the code, the name, the description, the priority, the tracking flag, the
/// conditions, and the limit rows. A phase of another machine and a deleted phase are "not found".
/// </summary>
/// <remarks>
/// The handler never queries for another phase with the same code or priority. The unique indexes are the only
/// guard, and the handler maps their failure to the conflict. A parallel change of the same phase fails on the row
/// version (<c>xmin</c>) check and gives the same kind of conflict.
/// </remarks>
/// <param name="dbContext">Loads the definitions and the phase, and saves the change.</param>
public sealed class UpdateOperatingPhaseRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<UpdateOperatingPhaseRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        UpdateOperatingPhaseRequest request,
        CancellationToken cancellationToken
    )
    {
        var phase = await _dbContext.OperatingPhases
                .Include(candidate => candidate.Conditions)
                .Include(candidate => candidate.Limits)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.MachineRegistrationId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(OperatingPhaseErrors.PhaseNotFound);

        var (conditions, limits) = await _dbContext.BuildPhaseBodyAsync(request, request.MachineId, cancellationToken);

        phase.ChangeCode(request.Code);
        var isTrackedChanged = phase.Update(request.Name, request.Description, request.IsTracked);
        phase.ChangePriority(request.Priority);
        var isMeaningChanged = phase.Replace(conditions, limits);

        if (isTrackedChanged || isMeaningChanged)
        {
            await _dbContext.RemovePhaseStatisticsAsync(phase.Id, cancellationToken);
        }

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
            throw new ConflictException(OperatingPhaseErrors.PhaseChanged, exception);
        }
    }
}
