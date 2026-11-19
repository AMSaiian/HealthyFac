using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record GetOperatingPhaseRequest : IRequest<OperatingPhaseResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class GetOperatingPhaseRequestValidator : AbstractValidator<GetOperatingPhaseRequest>
{
    public GetOperatingPhaseRequestValidator()
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
public sealed class GetOperatingPhasePreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetOperatingPhaseRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetOperatingPhaseRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Gets one phase of a machine with its conditions and limit rows. A phase of another machine and a deleted phase are
/// "not found".
/// </summary>
/// <param name="dbContext">Reads the phase.</param>
public class GetOperatingPhaseRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetOperatingPhaseRequest, OperatingPhaseResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<OperatingPhaseResponse> HandleAsync(
        GetOperatingPhaseRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var phase = await _dbContext.OperatingPhases
                .AsNoTracking()
                .Include(phase => phase.Conditions)
                .Include(phase => phase.Limits)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.MachineRegistrationId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(OperatingPhaseErrors.PhaseNotFound);

        return OperatingPhaseResponse.From(phase);
    }
}
