using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record DeleteOperatingPhaseRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class DeleteOperatingPhaseRequestValidator : AbstractValidator<DeleteOperatingPhaseRequest>
{
    public DeleteOperatingPhaseRequestValidator()
    {
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
public sealed class DeleteOperatingPhasePreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteOperatingPhaseRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DeleteOperatingPhaseRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Removes a phase. The save interceptor turns the removal into a soft delete, so the row stays and the query filter
/// hides it. A phase of another machine and a phase that is already deleted are "not found".
/// </summary>
/// <param name="dbContext">Loads the phase and saves the removal.</param>
public sealed class DeleteOperatingPhaseRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<DeleteOperatingPhaseRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        DeleteOperatingPhaseRequest request,
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

        await _dbContext.RemovePhaseStatisticsAsync(phase.Id, cancellationToken);
        _dbContext.OperatingPhases.Remove(phase);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
