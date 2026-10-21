using HealthyFac.Core.Domain.Machines;
using HealthyFac.Shared.Application.UseCases;

namespace HealthyFac.Core.Application.Machines.CreateMachine;

public record CreateMachineRequest : IRequest<Guid>
{
    public required string Name { get; init; }
    public required string SerialNumber { get; init; }
    public required Guid TenantId { get; init; }
}

public class CreateMachineRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<CreateMachineRequest, Guid>
{
    public async Task<Guid> HandleAsync(
        CreateMachineRequest request,
        CancellationToken ct = default(CancellationToken)
    )
    {
        var machine = new Machine
        {
            Name = request.Name,
            SerialNumber = request.SerialNumber,
            TenantId = request.TenantId,
        };

        dbContext.Machines.Add(machine);
        await dbContext.SaveChangesAsync(ct);

        return machine.Id;
    }
}
