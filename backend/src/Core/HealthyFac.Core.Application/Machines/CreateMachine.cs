using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines;

public record CreateMachineRequest : IRequest<CreateMachineResponse>
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid DepartmentId { get; init; }

    public string? Description { get; init; }
}

public record CreateMachineResponse
{
    public required Guid Id { get; init; }
}

public class CreateMachineRequestValidator : AbstractValidator<CreateMachineRequest>
{
    public CreateMachineRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(x => x.Name).IsMachineName();

        RuleFor(x => x.SerialNumber).IsMachineSerialNumber();

        RuleFor(x => x.Description).IsMachineDescription(richTextPolicy);

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(MachineErrors.CompanyIdRequired);

        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithMessage(MachineErrors.DepartmentIdRequired);
    }
}

public sealed class CreateMachinePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CreateMachineRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CreateMachineRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);
        actor.EnsureDepartments([request.DepartmentId]);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Creates a machine in a department of the company. The serial number must be unique among the machines of the
/// company that are not deleted, ignoring case.
/// </summary>
/// <remarks>
/// The handler checks the serial number first, so the common conflict costs no failed insert. The unique index
/// catches a parallel create of the same serial number, and the handler maps that failure to the same conflict.
/// </remarks>
/// <param name="dbContext">Reads the department and the serial numbers and saves the machine.</param>
public class CreateMachineRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<CreateMachineRequest, CreateMachineResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<CreateMachineResponse> HandleAsync(
        CreateMachineRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var isDepartmentInCompany = await _dbContext.Departments.AnyAsync(
            department => department.Id == request.DepartmentId && department.CompanyId == request.CompanyId,
            cancellationToken);

        if (!isDepartmentInCompany)
        {
            throw new NotFoundException(MachineErrors.DepartmentNotFound);
        }

        var machine = new Machine(
            request.CompanyId,
            request.DepartmentId,
            request.Name,
            request.SerialNumber,
            request.Description);

        var isSerialNumberTaken = await _dbContext.Machines.AnyAsync(
            candidate => candidate.CompanyId == machine.CompanyId
                && candidate.SerialNumber == machine.SerialNumber,
            cancellationToken);

        if (isSerialNumberTaken)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken);
        }

        _dbContext.Machines.Add(machine);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(MachineErrors.SerialNumberTaken, exception);
        }

        return new CreateMachineResponse { Id = machine.Id };
    }
}
