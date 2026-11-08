using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.StatusDefinitions;

public record GetStatusDefinitionRequest : IRequest<StatusDefinitionResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class GetStatusDefinitionRequestValidator : AbstractValidator<GetStatusDefinitionRequest>
{
    public GetStatusDefinitionRequestValidator()
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
public sealed class GetStatusDefinitionPreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetStatusDefinitionRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetStatusDefinitionRequest request, CancellationToken cancellationToken)
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
/// Gets one status definition of a machine. The pre-handler check has already checked the machine scope. A status
/// definition of another machine and a deleted status definition are "not found".
/// </summary>
/// <param name="dbContext">Reads the status definition.</param>
public class GetStatusDefinitionRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetStatusDefinitionRequest, StatusDefinitionResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<StatusDefinitionResponse> HandleAsync(
        GetStatusDefinitionRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.StatusDefinitions
            .AsNoTracking()
            .Where(status => status.Id == request.Id && status.MachineRegistrationId == request.MachineId)
            .Select(StatusDefinitionResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException(StatusDefinitionErrors.StatusDefinitionNotFound);
    }
}
