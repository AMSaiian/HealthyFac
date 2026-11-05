using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.MachineErrorCodes;

public record UpdateMachineErrorCodeRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid Id { get; init; }

    public required string Code { get; init; }

    public string? Description { get; init; }
}

public class UpdateMachineErrorCodeRequestValidator : AbstractValidator<UpdateMachineErrorCodeRequest>
{
    public UpdateMachineErrorCodeRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Code).IsMachineErrorCode();

        RuleFor(x => x.Description).IsMachineErrorCodeDescription(richTextPolicy);

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
public sealed class UpdateMachineErrorCodePreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UpdateMachineErrorCodeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(UpdateMachineErrorCodeRequest request, CancellationToken cancellationToken)
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
/// Changes the code and the description of an error code. The pre-handler check has already checked the machine
/// scope. An error code of another machine and a deleted error code are "not found".
/// </summary>
/// <remarks>
/// The handler never queries for another error code with the same code. The unique index on the machine and the
/// code is the only guard, and the handler maps its failure to the conflict.
/// </remarks>
/// <param name="dbContext">Loads and saves the error code.</param>
public sealed class UpdateMachineErrorCodeRequestHandler(ICoreDbContext dbContext)
    : RequestHandler<UpdateMachineErrorCodeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        UpdateMachineErrorCodeRequest request,
        CancellationToken cancellationToken
    )
    {
        var errorCode = await _dbContext.MachineErrorCodes
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.MachineId == request.MachineId,
                    cancellationToken)
            ?? throw new NotFoundException(MachineErrorCodeErrors.ErrorCodeNotFound);

        errorCode.Update(request.Code, request.Description);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrorCodeErrors.CodeTaken, exception);
        }
    }
}
