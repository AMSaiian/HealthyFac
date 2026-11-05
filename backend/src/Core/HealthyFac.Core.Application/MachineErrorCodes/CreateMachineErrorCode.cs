using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Domain.MachineErrorCodes;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.MachineErrorCodes;

public record CreateMachineErrorCodeRequest : IRequest<CreateMachineErrorCodeResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required string Code { get; init; }

    public string? Description { get; init; }
}

public record CreateMachineErrorCodeResponse
{
    public required Guid Id { get; init; }
}

public class CreateMachineErrorCodeRequestValidator : AbstractValidator<CreateMachineErrorCodeRequest>
{
    public CreateMachineErrorCodeRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Code).IsMachineErrorCode();

        RuleFor(x => x.Description).IsMachineErrorCodeDescription(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(MachineErrorCodeErrors.MachineIdRequired);
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company or the departments of the actor, and a
/// deleted machine, are "not found" (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company and the departments of the actor.</param>
public sealed class CreateMachineErrorCodePreHandlerCheck(ICoreDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CreateMachineErrorCodeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(CreateMachineErrorCodeRequest request, CancellationToken cancellationToken)
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
/// Creates an error code for a machine. The code must be unique among the error codes of the machine that are not
/// deleted, ignoring case.
/// </summary>
/// <remarks>
/// The pre-handler check has already checked the machine scope. The handler never queries for an existing code. The
/// unique index on the machine and the code is the only guard, and the handler maps its failure to the conflict.
/// </remarks>
/// <param name="dbContext">Saves the error code.</param>
public class CreateMachineErrorCodeRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<CreateMachineErrorCodeRequest, CreateMachineErrorCodeResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<CreateMachineErrorCodeResponse> HandleAsync(
        CreateMachineErrorCodeRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var errorCode = new MachineErrorCode(request.MachineId, request.Code, request.Description);

        _dbContext.MachineErrorCodes.Add(errorCode);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrorCodeErrors.CodeTaken, exception);
        }

        return new CreateMachineErrorCodeResponse { Id = errorCode.Id };
    }
}
