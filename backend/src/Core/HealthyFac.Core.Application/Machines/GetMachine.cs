using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record GetMachineRequest : IRequest<MachineResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public sealed class GetMachinePreHandlerCheck(ICurrentUserProvider currentUser) : IPreHandlerCheck<GetMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetMachineRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one machine. A machine outside the company or the departments of the actor is "not found", so the answer
/// does not show that it exists.
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the departments of the actor.</param>
public sealed class GetMachineRequestHandler(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IRequestHandler<GetMachineRequest, MachineResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<MachineResponse> HandleAsync(
        GetMachineRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var departmentIds = _currentUser.GetActor().DepartmentIds.ToList();

        var machine = await _dbContext.Machines
            .AsNoTracking()
            .Where(candidate => candidate.Id == request.Id
                && candidate.CompanyId == request.CompanyId
                && departmentIds.Contains(candidate.DepartmentId))
            .Select(MachineResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return machine ?? throw new NotFoundException(MachineErrors.MachineNotFound);
    }
}
