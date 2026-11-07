using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Application.StatusDefinitions.SearchStatusDefinitions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;

/// <summary>
/// Arranges status definition data and runs the status definition use cases as a user with a company and a set of
/// departments. Every method opens its own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class StatusDefinitionFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public Task<Guid> CreateStatusDefinitionMachineAsync(MachineTenant tenant) =>
            fixture.RegisterMachineInDepartmentAsync(tenant.CompanyId, tenant.FirstDepartmentId);

        public async Task<Guid> CreateStatusDefinitionAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            string code,
            string? description = null,
            StatusSeverity? severity = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateStatusDefinitionRequest, CreateStatusDefinitionResponse>>();

            var response = await handler.HandleAsync(
                new CreateStatusDefinitionRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    Code = code,
                    Severity = severity ?? StatusSeverity.Info,
                    Description = description,
                });

            return response.Id;
        }

        public async Task<StatusDefinitionResponse> GetStatusDefinitionAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid statusId
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetStatusDefinitionRequest, StatusDefinitionResponse>>();

            return await handler.HandleAsync(
                new GetStatusDefinitionRequest { CompanyId = companyId, MachineId = machineId, Id = statusId });
        }

        public async Task UpdateStatusDefinitionAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid statusId,
            string code,
            string? description = null,
            StatusSeverity? severity = null
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<UpdateStatusDefinitionRequest>>();

            await handler.HandleAsync(
                new UpdateStatusDefinitionRequest
                {
                    CompanyId = companyId,
                    MachineId = machineId,
                    Id = statusId,
                    Code = code,
                    Severity = severity ?? StatusSeverity.Info,
                    Description = description,
                });
        }

        public async Task DeleteStatusDefinitionAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            Guid machineId,
            Guid statusId
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<DeleteStatusDefinitionRequest>>();

            await handler.HandleAsync(
                new DeleteStatusDefinitionRequest { CompanyId = companyId, MachineId = machineId, Id = statusId });
        }

        public async Task<GridResponse<StatusDefinitionResponse>> SearchStatusDefinitionsAsync(
            Guid companyId,
            IEnumerable<Guid> actorDepartmentIds,
            SearchStatusDefinitionsRequest request
        )
        {
            using var scope = fixture.CreateUserScope(companyId, actorDepartmentIds);
            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<SearchStatusDefinitionsRequest,
                    GridResponse<StatusDefinitionResponse>>>();

            return await handler.HandleAsync(request);
        }

        public async Task<IReadOnlyList<Guid>> SeedStatusDefinitionsAsync(
            Guid machineId,
            IReadOnlyList<StatusDefinitionSeed> seeds
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var statuses = seeds
                .Select(seed => new StatusDefinition(machineId, seed.Code, seed.Severity, description: null))
                .ToList();

            dbContext.StatusDefinitions.AddRange(statuses);
            await dbContext.SaveChangesAsync();

            foreach (var (status, seed) in statuses.Zip(seeds))
            {
                await dbContext.StatusDefinitions
                    .Where(candidate => candidate.Id == status.Id)
                    .ExecuteUpdateAsync(update => update
                        .SetProperty(candidate => candidate.CreatedAt, seed.CreatedAt)
                        .SetProperty(candidate => candidate.UpdatedAt, seed.UpdatedAt));
            }

            return statuses.Select(status => status.Id).ToList();
        }

        public async Task<StatusDefinition> LoadStatusDefinitionRowAsync(Guid statusId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .StatusDefinitions
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleAsync(status => status.Id == statusId);
        }

        public async Task<IReadOnlyList<StatusDefinition>> LoadStatusDefinitionRowsAsync(Guid machineId, string code)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .StatusDefinitions
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .Where(status => status.MachineRegistrationId == machineId && status.Code == code)
                .ToListAsync();
        }

        public async Task RemoveStatusDefinitionRowAsync(MachineTenant tenant, Guid statusId)
        {
            using var scope = fixture.CreateUserScope(tenant.CompanyId, tenant.BothDepartmentIds);
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var status = await dbContext.StatusDefinitions.SingleAsync(candidate => candidate.Id == statusId);

            dbContext.StatusDefinitions.Remove(status);
            await dbContext.SaveChangesAsync();
        }
    }
}

public sealed record StatusDefinitionSeed(
    string Code,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    StatusSeverity Severity
);
