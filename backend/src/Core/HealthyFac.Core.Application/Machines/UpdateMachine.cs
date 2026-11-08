using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record UpdateMachineRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required string Name { get; init; }

    public required string SerialNumber { get; init; }

    public string? Description { get; init; }
}

public class UpdateMachineRequestValidator : AbstractValidator<UpdateMachineRequest>
{
    public UpdateMachineRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Name).IsMachineName();

        RuleFor(x => x.SerialNumber).IsMachineSerialNumber();

        RuleFor(x => x.Description).IsMachineDescription(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(MachineErrors.MachineIdRequired);
    }
}

public sealed class UpdateMachinePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UpdateMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(UpdateMachineRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Changes the name, serial number, and description of a machine. A machine outside
/// the company is "not found". The serial number must stay unique among the
/// machines of the company that are not deleted, ignoring case.
/// </summary>
/// <remarks>
/// The handler checks the serial number against the other machines first. The unique index catches a parallel
/// change, and the handler maps that failure to the same conflict.
/// </remarks>
/// <param name="dbContext">Loads and saves the machine.</param>
public sealed class UpdateMachineRequestHandler(ICoreDbContext dbContext)
    : RequestHandler<UpdateMachineRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        UpdateMachineRequest request,
        CancellationToken cancellationToken
    )
    {
        var machine = await _dbContext.Machines.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(MachineErrors.MachineNotFound);

        machine.Update(request.Name, request.SerialNumber, request.Description);

        var isSerialNumberTaken = await _dbContext.Machines.AnyAsync(
            candidate => candidate.CompanyId == machine.CompanyId
                && candidate.SerialNumber == machine.SerialNumber
                && candidate.Id != machine.Id,
            cancellationToken);

        if (isSerialNumberTaken)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken);
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken, exception);
        }
    }
}
