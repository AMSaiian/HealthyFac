using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Machines.CreateMachine;

public record CreateMachineRequest : IRequest<CreateMachineResponse>
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid CompanyId { get; init; }
    public required Guid DepartmentId { get; init; }
}

public record CreateMachineResponse
{
    public required Guid Id { get; init; }
}

public class CreateMachineRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<CreateMachineRequest, CreateMachineResponse>
{
    public async Task<CreateMachineResponse> HandleAsync(
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

        return new CreateMachineResponse { Id = machine.Id };
    }
}
