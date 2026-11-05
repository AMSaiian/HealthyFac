using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Application.Machines.SearchMachines;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Machines.Data;

/// <summary>
/// Arranges machine data and runs the machine use cases as a user with a company and a set of departments. Every
/// method opens its own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class MachineFixtureExtensions
{
    private static Machine CreateMachine(Guid companyId, Guid departmentId, MachineSeed seed)
    {
        var machine = new Machine(companyId, departmentId, seed.Name, seed.SerialNumber, null, seed.CreatedAt);

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
            var departmentIds = await fixture.CreateDepartmentsAsync(
                companyId,
                MachineTestData.FirstDepartmentName,
                MachineTestData.SecondDepartmentName);

            return new MachineTenant(companyId, departmentIds[0], departmentIds[1]);
        }

        public async Task<Guid> CreateMachineAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid departmentId,
            string name,
            string serialNumber,
            string? description = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateMachineRequest, CreateMachineResponse>>();

            var response = await handler.HandleAsync(
                new CreateMachineRequest
                {
                    CompanyId = companyId,
                    DepartmentId = departmentId,
                    Name = name,
                    SerialNumber = serialNumber,
                    Description = description,
                });

            return response.Id;
        }

        public async Task UpdateMachineAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid departmentId,
            string name,
            string serialNumber,
            string? description = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<UpdateMachineRequest>>();

            await handler.HandleAsync(
                new UpdateMachineRequest
                {
                    CompanyId = companyId,
                    Id = machineId,
                    DepartmentId = departmentId,
                    Name = name,
                    SerialNumber = serialNumber,
                    Description = description,
                });
        }

        public async Task DeleteMachineAsync(Guid companyId, IEnumerable<Guid> actorDepartmentIds, Guid machineId)
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteMachineRequest>>();

            await handler.HandleAsync(new DeleteMachineRequest { CompanyId = companyId, Id = machineId });
        }

        public async Task<MachineResponse> GetMachineAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetMachineRequest, MachineResponse>>();

            return await handler.HandleAsync(new GetMachineRequest { CompanyId = companyId, Id = machineId });
        }

        public async Task<GridResponse<MachineResponse>> SearchMachinesAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            SearchMachinesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchMachinesRequest, GridResponse<MachineResponse>>>();

            return await handler.HandleAsync(request);
        }

        public async Task<IReadOnlyList<Guid>> SeedMachinesAsync(
            Guid companyId,
            Guid departmentId,
            IReadOnlyList<MachineSeed> seeds
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var machines = seeds.Select(seed => CreateMachine(companyId, departmentId, seed)).ToList();

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
                .IgnoreQueryFilters()
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
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(machine => machine.CompanyId == companyId && machine.SerialNumber == serialNumber)
                .ToListAsync();
        }

        public async Task<Machine> LoadMachineRowAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Machines
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(machine => machine.Id == machineId);
        }
    }
}

public sealed record MachineTenant(Guid CompanyId, Guid FirstDepartmentId, Guid SecondDepartmentId)
{
    public IReadOnlyList<Guid> BothDepartmentIds => [FirstDepartmentId, SecondDepartmentId];
}
