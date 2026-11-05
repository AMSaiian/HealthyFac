using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.MachineErrorCodes;

public record DeleteMachineErrorCodeRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }
}

public class DeleteMachineErrorCodeRequestValidator : AbstractValidator<DeleteMachineErrorCodeRequest>
{
    public DeleteMachineErrorCodeRequestValidator()
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
public sealed class DeleteMachineErrorCodePreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteMachineErrorCodeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(DeleteMachineErrorCodeRequest request, CancellationToken cancellationToken)
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
/// Removes an error code. The save interceptor turns the removal into a soft delete, so the row stays and the query
/// filter hides it. The pre-handler check has already checked the machine scope. An error code of another machine
/// and an error code that is already deleted are "not found".
/// </summary>
/// <param name="dbContext">Loads the error code and saves the removal.</param>
public sealed class DeleteMachineErrorCodeRequestHandler(ICoreDbContext dbContext)
    : RequestHandler<DeleteMachineErrorCodeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        DeleteMachineErrorCodeRequest request,
        CancellationToken cancellationToken
    )
    {
        var errorCode = await _dbContext.MachineErrorCodes
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.MachineId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(MachineErrorCodeErrors.ErrorCodeNotFound);

        _dbContext.MachineErrorCodes.Remove(errorCode);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
