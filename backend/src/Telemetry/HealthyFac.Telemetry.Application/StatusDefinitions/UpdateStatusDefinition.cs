using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

public record UpdateStatusDefinitionRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public required StatusSeverity? Severity { get; init; }

    public string? Description { get; init; }
}

public class UpdateStatusDefinitionRequestValidator : AbstractValidator<UpdateStatusDefinitionRequest>
{
    public UpdateStatusDefinitionRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Code).IsStatusCode();

        RuleFor(x => x.Severity).IsStatusSeverity();

        RuleFor(x => x.Description).IsStatusDefinitionDescription(richTextPolicy);

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
public sealed class UpdateStatusDefinitionPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<UpdateStatusDefinitionRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(UpdateStatusDefinitionRequest request, CancellationToken cancellationToken)
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
/// Changes the code, the severity, and the description of a status definition. The pre-handler check has already
/// checked the machine scope. A status definition of another machine and a deleted status definition are "not found".
/// </summary>
/// <remarks>
/// The handler never queries for another status definition with the same code. The unique index on the machine and the
/// code is the only guard, and the handler maps its failure to the conflict.
/// </remarks>
/// <param name="dbContext">Loads and saves the status definition.</param>
public sealed class UpdateStatusDefinitionRequestHandler(ITelemetryDbContext dbContext)
    : RequestHandler<UpdateStatusDefinitionRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        UpdateStatusDefinitionRequest request,
        CancellationToken cancellationToken
    )
    {
        var status = await _dbContext.StatusDefinitions
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.MachineRegistrationId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(StatusDefinitionErrors.StatusDefinitionNotFound);

        status.Update(request.Code, request.Severity!, request.Description);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(StatusDefinitionErrors.CodeTaken, exception);
        }
    }
}
