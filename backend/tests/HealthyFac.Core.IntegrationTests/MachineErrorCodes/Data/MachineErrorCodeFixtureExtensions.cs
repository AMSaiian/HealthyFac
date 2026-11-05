using HealthyFac.Core.Application;
using HealthyFac.Core.Application.MachineErrorCodes;
using HealthyFac.Core.Application.MachineErrorCodes.SearchMachineErrorCodes;
using HealthyFac.Core.Domain.MachineErrorCodes;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Machines.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.MachineErrorCodes.Data;

/// <summary>
/// Arranges machine error code data and runs the error code use cases as a user with a company and a set of
/// departments. Every method opens its own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class MachineErrorCodeFixtureExtensions
{
    extension(CoreDatabaseFixture fixture)
    {
        public Task<Guid> CreateErrorCodeMachineAsync(
            MachineTenant tenant,
            string name = MachineErrorCodeTestData.FirstMachineName,
            string serialNumber = MachineErrorCodeTestData.FirstMachineSerialNumber
        ) =>
            fixture.CreateMachineAsync(
                tenant.CompanyId,
                tenant.BothDepartmentIds,
                tenant.FirstDepartmentId,
                name,
                serialNumber);

        public async Task<Guid> CreateMachineErrorCodeAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            string code,
            string? description = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateMachineErrorCodeRequest, CreateMachineErrorCodeResponse>>();

            var response = await handler.HandleAsync(
                new CreateMachineErrorCodeRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    Code = code,
                    Description = description,
                });

            return response.Id;
        }

        public async Task<MachineErrorCodeResponse> GetMachineErrorCodeAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid errorCodeId
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetMachineErrorCodeRequest, MachineErrorCodeResponse>>();

            return await handler.HandleAsync(
                new GetMachineErrorCodeRequest { CompanyId = companyId, MachineId = machineId, Id = errorCodeId });
        }

        public async Task UpdateMachineErrorCodeAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid errorCodeId,
            string code,
            string? description = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UpdateMachineErrorCodeRequest>>();

            await handler.HandleAsync(
                new UpdateMachineErrorCodeRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    Id = errorCodeId,
                    Code = code,
                    Description = description,
                });
        }

        public async Task DeleteMachineErrorCodeAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid errorCodeId
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<DeleteMachineErrorCodeRequest>>();

            await handler.HandleAsync(
                new DeleteMachineErrorCodeRequest { CompanyId = companyId, MachineId = machineId, Id = errorCodeId });
        }

        public async Task<GridResponse<MachineErrorCodeResponse>> SearchMachineErrorCodesAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            SearchMachineErrorCodesRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchMachineErrorCodesRequest,
                    GridResponse<MachineErrorCodeResponse>>>();

            return await handler.HandleAsync(request);
        }

        public async Task<IReadOnlyList<Guid>> SeedMachineErrorCodesAsync(
            Guid machineId,
            IReadOnlyList<MachineErrorCodeSeed> seeds
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var errorCodes = seeds
                .Select(seed => new MachineErrorCode(machineId, seed.Code, null))
                .ToList();

            dbContext.MachineErrorCodes.AddRange(errorCodes);
            await dbContext.SaveChangesAsync();

            foreach (var (errorCode, seed) in errorCodes.Zip(seeds))
            {
                await dbContext.MachineErrorCodes
                    .Where(candidate => candidate.Id == errorCode.Id)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(candidate => candidate.CreatedAt, seed.CreatedAt)
                        .SetProperty(candidate => candidate.UpdatedAt, seed.UpdatedAt));
            }

            return errorCodes.Select(errorCode => errorCode.Id).ToList();
        }

        public async Task<MachineErrorCode> LoadMachineErrorCodeRowAsync(Guid errorCodeId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .MachineErrorCodes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .SingleAsync(errorCode => errorCode.Id == errorCodeId);
        }

        public async Task<IReadOnlyList<MachineErrorCode>> LoadMachineErrorCodeRowsAsync(Guid machineId, string code)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .MachineErrorCodes
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Where(errorCode => errorCode.MachineId == machineId && errorCode.Code == code)
                .ToListAsync();
        }

        public async Task RemoveMachineErrorCodeRowAsync(MachineTenant tenant, Guid errorCodeId)
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId, tenant.BothDepartmentIds);
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();
            var errorCode = await dbContext.MachineErrorCodes.SingleAsync(candidate => candidate.Id == errorCodeId);

            dbContext.MachineErrorCodes.Remove(errorCode);
            await dbContext.SaveChangesAsync();
        }
    }
}

public sealed record MachineErrorCodeSeed(string Code, DateTime CreatedAt, DateTime? UpdatedAt);
