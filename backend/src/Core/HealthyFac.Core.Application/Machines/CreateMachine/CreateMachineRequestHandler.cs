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

        var machine = new Machine
        {
            Name = request.Name,
            SerialNumber = request.SerialNumber,
            CompanyId = request.CompanyId,
            DepartmentId = request.DepartmentId,
        };

        _dbContext.Machines.Add(machine);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new CreateMachineResponse { Id = machine.Id };
    }
}
