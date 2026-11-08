using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Domain.Gateways;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Machines.Data;

/// <summary>
/// Arranges machine data and runs the machine use cases as a user with a company. Every method
/// opens its own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class MachineFixtureExtensions
{
    private static Machine CreateMachine(Guid companyId, MachineSeed seed)
    {
        var machine = new Machine(
            companyId,
            GatewayIds.Create(),
            seed.Name,
            seed.SerialNumber,
            description: null,
            seed.CreatedAt);

        if (seed.State != MachineState.Operational)
        {
            machine.ChangeState(seed.State, seed.CreatedAt.AddHours(MachineTestData.StateChangeDelayHours));
        }

        return machine;
    }

    extension(CoreDatabaseFixture fixture)
    {
        public async Task<MachineTenant> ArrangeMachineTenantAsync(string companyName = MachineTestData.CompanyName)
        {
            var companyId = await fixture.CreateCompanyAsync(companyName);

            return new MachineTenant(companyId);
        }

        public async Task<Guid> CreateMachineAsync(
            Guid companyId,
            string name,
            string serialNumber,
            string? description = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateMachineRequest, CreateMachineResponse>>();

            var response = await handler.HandleAsync(
                new CreateMachineRequest
                {
                    CompanyId = companyId,
                    Name = name,
                    SerialNumber = serialNumber,
                    Description = description,
                });

            return response.Id;
        }

        public async Task UpdateMachineAsync(
            Guid companyId,
            Guid machineId,
            string name,
            string serialNumber,
            string? description = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<UpdateMachineRequest>>();

            await handler.HandleAsync(
                new UpdateMachineRequest
                {
                    CompanyId = companyId,
                    Id = machineId,
                    Name = name,
                    SerialNumber = serialNumber,
                    Description = description,
                });
        }

        public async Task DeleteMachineAsync(Guid companyId, Guid machineId)
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteMachineRequest>>();

            await handler.HandleAsync(new DeleteMachineRequest { CompanyId = companyId, Id = machineId });
        }

        public async Task<MachineResponse> GetMachineAsync(
            Guid companyId,
            Guid machineId
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetMachineRequest, MachineResponse>>();

            return await handler.HandleAsync(new GetMachineRequest { CompanyId = companyId, Id = machineId });
        }

        public async Task<GridResponse<MachineResponse>> SearchMachinesAsync(
            Guid companyId,
            SearchMachinesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchMachinesRequest, GridResponse<MachineResponse>>>();

            return await handler.HandleAsync(request);
        }

        public async Task<IReadOnlyList<Guid>> SeedMachinesAsync(
            Guid companyId,
            IReadOnlyList<MachineSeed> seeds
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var machines = seeds.Select(seed => CreateMachine(companyId, seed)).ToList();

            dbContext.Machines.AddRange(machines);
            await dbContext.SaveChangesAsync();

            foreach (var (machine, seed) in machines.Zip(seeds))
            {
                await dbContext.Machines
                    .Where(candidate => candidate.Id == machine.Id)
                    .ExecuteUpdateAsync(update => update.SetProperty(candidate => candidate.CreatedAt, seed.CreatedAt));
            }

            return machines.Select(machine => machine.Id).ToList();
        }

        public async Task AddMachineWithGatewayIdAsync(
            Guid companyId,
            string gatewayId,
            string serialNumber
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            dbContext.Machines.Add(
                new Machine(
                    companyId,
                    gatewayId,
                    MachineTestData.MachineName,
                    serialNumber,
                    description: null,
                    TimeProvider.System.GetUtcNow().UtcDateTime));
            await dbContext.SaveChangesAsync();
        }

        public async Task AddStateRowAsync(Guid machineId, MachineState state, DateTime startedAt)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            dbContext.MachineStateHistories.Add(new MachineStateHistory(machineId, state, startedAt));
            await dbContext.SaveChangesAsync();
        }

        public async Task SoftDeleteStateRowAsync(Guid machineId, DateTime startedAt)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var row = await dbContext.MachineStateHistories.SingleAsync(candidate =>
                candidate.MachineId == machineId && candidate.StartedAt == startedAt);

            dbContext.MachineStateHistories.Remove(row);
            await dbContext.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<MachineStateHistory>> LoadStateHistoryAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .MachineStateHistories
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(row => row.MachineId == machineId)
                .OrderBy(row => row.StartedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<Machine>> LoadMachineRowsAsync(Guid companyId, string serialNumber)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Machines
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(machine => machine.CompanyId == companyId && machine.SerialNumber == serialNumber)
                .ToListAsync();
        }

        public async Task<Machine> LoadMachineRowAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Machines
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleAsync(machine => machine.Id == machineId);
        }
    }
}

public sealed record MachineTenant(Guid CompanyId);
