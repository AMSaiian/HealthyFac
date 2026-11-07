using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;

/// <summary>
/// Runs the registration use cases and reads the registrations. Every method opens its own DI scope, so it reads
/// what is stored, not tracked objects.
/// </summary>
public static class MachineRegistrationFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public MachineTenant ArrangeMachineTenant() =>
            new(Guid.CreateVersion7(), Guid.CreateVersion7(), Guid.CreateVersion7());

        public async Task<Guid> RegisterMachineInDepartmentAsync(Guid companyId, Guid departmentId)
        {
            var message = MachineRegistrationTestData.CreateMessage() with
            {
                CompanyId = companyId,
                DepartmentId = departmentId,
            };

            await fixture.RegisterMachineAsync(message);

            return message.MachineId;
        }

        public Task DeleteMachineAsync(Guid companyId, IEnumerable<Guid> actorDepartmentIds, Guid machineId) =>
            fixture.UnregisterMachineAsync(new MachineDeleted { MachineId = machineId, CompanyId = companyId });

        public async Task RegisterMachineAsync(MachineCreated message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineCreated>>();

            await handler.HandleAsync(message);
        }

        public async Task UnregisterMachineAsync(MachineDeleted message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineDeleted>>();

            await handler.HandleAsync(message);
        }

        public async Task<Guid> AddMetricDefinitionAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var unit = await dbContext.MeasurementUnits
                .Include(candidate => candidate.QuantityKind)
                .Where(candidate => candidate.QuantityKind.Name == "Temperature" && candidate.Name == "DegreeCelsius")
                .SingleAsync();
            var metric = new MetricDefinition(
                machineId,
                key: "spindle.temperature",
                name: "Spindle temperature",
                description: null,
                MetricValueKind.Numeric,
                unit,
                min: -20,
                max: 200,
                states: null);

            dbContext.MetricDefinitions.Add(metric);
            await dbContext.SaveChangesAsync();

            return metric.Id;
        }

        public async Task<MetricDefinition> LoadMetricDefinitionRowAsync(Guid metricId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricDefinitions
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleAsync(metric => metric.Id == metricId);
        }

        public async Task<IReadOnlyList<MetricSchemaRevision>> LoadSchemaRevisionsAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricSchemaRevisions
                .AsNoTracking()
                .Where(revision => revision.MachineRegistrationId == machineId)
                .OrderBy(revision => revision.Revision)
                .ToListAsync();
        }

        public async Task<MachineRegistration?> LoadRegistrationAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleOrDefaultAsync(registration => registration.Id == machineId);
        }

        public async Task<int> CountRegistrationsAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .CountAsync(registration => registration.Id == machineId);
        }
    }
}

public sealed record MachineTenant(Guid CompanyId, Guid FirstDepartmentId, Guid SecondDepartmentId)
{
    public IReadOnlyList<Guid> BothDepartmentIds => [FirstDepartmentId, SecondDepartmentId];
}
