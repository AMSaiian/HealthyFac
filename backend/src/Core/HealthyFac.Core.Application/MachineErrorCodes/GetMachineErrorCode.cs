using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.MachineErrorCodes;

public record GetMachineErrorCodeRequest : IRequest<MachineErrorCodeResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class GetMachineErrorCodeRequestValidator : AbstractValidator<GetMachineErrorCodeRequest>
{
    public GetMachineErrorCodeRequestValidator()
    {
        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.MachineIdRequired);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.ErrorCodeIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company or the departments of the actor, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company and the departments of the actor.</param>
public sealed class GetMachineErrorCodePreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetMachineErrorCodeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetMachineErrorCodeRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(
            request.CompanyId,
            actor.DepartmentIds.ToList(),
            request.MachineId,
            cancellationToken);
    }
}

/// <summary>
/// Gets one error code of a machine. The pre-handler check has already checked the machine scope. An error code of
/// another machine and a deleted error code are "not found".
/// </summary>
/// <param name="dbContext">Reads the error code.</param>
public class GetMachineErrorCodeRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<GetMachineErrorCodeRequest, MachineErrorCodeResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<MachineErrorCodeResponse> HandleAsync(
        GetMachineErrorCodeRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.MachineErrorCodes
            .AsNoTracking()
            .Where(errorCode => errorCode.Id == request.Id && errorCode.MachineId == request.MachineId)
            .Select(MachineErrorCodeResponse.Projection)
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException(MachineErrorCodeErrors.ErrorCodeNotFound);
    }
}
