using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Telemetry.Application.OperatingPhases;

public record ListOperatingPhasesRequest : IRequest<ListOperatingPhasesResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }
}

public record ListOperatingPhasesResponse
{
    public required IReadOnlyList<OperatingPhaseResponse> Items { get; init; }
}

public class ListOperatingPhasesRequestValidator : AbstractValidator<ListOperatingPhasesRequest>
{
    public ListOperatingPhasesRequestValidator()
    {
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
public sealed class ListOperatingPhasesPreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ListOperatingPhasesRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(ListOperatingPhasesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Lists the phases of a machine in the order of the priority. A machine has a few phases, so the list is not
/// paged.
/// </summary>
/// <param name="dbContext">Reads the phases.</param>
public class ListOperatingPhasesRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<ListOperatingPhasesRequest, ListOperatingPhasesResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<ListOperatingPhasesResponse> HandleAsync(
        ListOperatingPhasesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var phases = await _dbContext.OperatingPhases
            .AsNoTracking()
            .Include(phase => phase.Conditions)
            .Include(phase => phase.Limits)
            .Where(phase => phase.MachineRegistrationId == request.MachineId)
            .OrderBy(phase => phase.Priority)
            .ToListAsync(cancellationToken);

        return new ListOperatingPhasesResponse { Items = phases.Select(OperatingPhaseResponse.From).ToList() };
    }
}
