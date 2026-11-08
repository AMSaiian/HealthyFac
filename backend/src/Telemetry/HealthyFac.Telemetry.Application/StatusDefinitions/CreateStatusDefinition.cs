using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

public record CreateStatusDefinitionRequest : IRequest<CreateStatusDefinitionResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required string Code { get; init; }

    public required StatusSeverity? Severity { get; init; }

    public string? Description { get; init; }
}

public record CreateStatusDefinitionResponse
{
    public required Guid Id { get; init; }
}

public class CreateStatusDefinitionRequestValidator : AbstractValidator<CreateStatusDefinitionRequest>
{
    public CreateStatusDefinitionRequestValidator(IRichTextPolicy richTextPolicy)
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
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class CreateStatusDefinitionPreHandlerCheck(
    ITelemetryDbContext dbContext,
    ICurrentUserProvider currentUser
)
    : IPreHandlerCheck<CreateStatusDefinitionRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(CreateStatusDefinitionRequest request, CancellationToken cancellationToken)
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
/// Creates a status definition for a machine. The code must be unique among the status definitions of the machine that
/// are not deleted, ignoring case.
/// </summary>
/// <remarks>
/// The pre-handler check has already checked the machine scope. The handler never queries for an existing code. The
/// unique index on the machine and the code is the only guard, and the handler maps its failure to the conflict.
/// </remarks>
/// <param name="dbContext">Saves the status definition.</param>
public class CreateStatusDefinitionRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<CreateStatusDefinitionRequest, CreateStatusDefinitionResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<CreateStatusDefinitionResponse> HandleAsync(
        CreateStatusDefinitionRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var status = new StatusDefinition(request.MachineId, request.Code, request.Severity!, request.Description);

        _dbContext.StatusDefinitions.Add(status);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(StatusDefinitionErrors.CodeTaken, exception);
        }

        return new CreateStatusDefinitionResponse { Id = status.Id };
    }
}
