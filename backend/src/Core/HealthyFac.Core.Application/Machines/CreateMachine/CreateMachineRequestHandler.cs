using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines.CreateMachine;

public record CreateMachineRequest : IRequest<Guid>
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid DepartmentId { get; init; }
}

public class CreateMachineRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<CreateMachineRequest, Guid>
{
    public async Task<Guid> HandleAsync(
        CreateMachineRequest request,
        CancellationToken ct = default(CancellationToken)
    )
    {
        var isDepartmentInCompany = await dbContext.Departments.AnyAsync(
            department => department.Id == request.DepartmentId && department.CompanyId == request.CompanyId,
            ct);

        if (!isDepartmentInCompany)
        {
            throw new NotFoundException(MachineErrors.DepartmentNotFound);
        }

        var machine = new Machine
        {
            Name = request.Name,
            SerialNumber = request.SerialNumber,
            CompanyId = request.CompanyId,
            DepartmentId = request.DepartmentId,
        };

        dbContext.Machines.Add(machine);
        await dbContext.SaveChangesAsync(ct);

        return machine.Id;
    }
}
