using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

public record DeleteStatusDefinitionRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class DeleteStatusDefinitionRequestValidator : AbstractValidator<DeleteStatusDefinitionRequest>
{
    public DeleteStatusDefinitionRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.MachineIdRequired);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(StatusDefinitionErrors.StatusDefinitionIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class DeleteStatusDefinitionPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<DeleteStatusDefinitionRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DeleteStatusDefinitionRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(
            request.CompanyId,
            request.MachineId,
            cancellationToken);
    }
}

/// <summary>
/// Removes a status definition. The save interceptor turns the removal into a soft delete, so the row stays and the
/// query filter hides it. The pre-handler check has already checked the machine scope. A status definition of another
/// machine and a status definition that is already deleted are "not found".
/// </summary>
/// <param name="dbContext">Loads the status definition and saves the removal.</param>
public sealed class DeleteStatusDefinitionRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<DeleteStatusDefinitionRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        DeleteStatusDefinitionRequest request,
        CancellationToken cancellationToken
    )
    {
        var status = await _dbContext.StatusDefinitions
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.MachineRegistrationId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(StatusDefinitionErrors.StatusDefinitionNotFound);

        _dbContext.StatusDefinitions.Remove(status);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
