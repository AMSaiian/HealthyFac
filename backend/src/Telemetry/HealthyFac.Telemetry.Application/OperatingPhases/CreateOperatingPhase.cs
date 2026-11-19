using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.OperatingPhases;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record CreateOperatingPhaseRequest : OperatingPhaseInput, IRequest<CreateOperatingPhaseResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

public record CreateOperatingPhaseResponse
{
    public required Guid Id { get; init; }
}

public class CreateOperatingPhaseRequestValidator : AbstractValidator<CreateOperatingPhaseRequest>
{
    public CreateOperatingPhaseRequestValidator(IRichTextPolicy richTextPolicy)
    {
        this.AddOperatingPhaseInputRules(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(OperatingPhaseErrors.MachineIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a deleted machine, are "not found"
/// (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class CreateOperatingPhasePreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CreateOperatingPhaseRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(CreateOperatingPhaseRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Creates a phase of a machine with its conditions and limit rows. The conditions and the limit rows must use
/// definitions of the same machine (<see cref="OperatingPhaseBodyBuilder"/>).
/// </summary>
/// <remarks>
/// The handler never queries for an existing code or priority. The two unique indexes of the active phases of the
/// machine are the only guards, and the handler maps their failure to the conflict.
/// </remarks>
/// <param name="dbContext">Reads the definitions and saves the phase.</param>
public class CreateOperatingPhaseRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<CreateOperatingPhaseRequest, CreateOperatingPhaseResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<CreateOperatingPhaseResponse> HandleAsync(
        CreateOperatingPhaseRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
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

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(OperatingPhaseErrors.CodeOrPriorityTaken, exception);
        }

        return new CreateOperatingPhaseResponse { Id = phase.Id };
    }
}
