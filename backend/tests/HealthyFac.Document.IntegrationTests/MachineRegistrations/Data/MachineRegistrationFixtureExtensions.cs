using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Document.Application;
using HealthyFac.Document.Domain.MachineRegistrations;
using HealthyFac.Document.IntegrationTests.Database.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Document.IntegrationTests.MachineRegistrations.Data;

/// <summary>
/// Runs the registration use cases and reads the registrations. Every method opens its own DI scope, so it reads
/// what is stored, not tracked objects.
/// </summary>
public static class MachineRegistrationFixtureExtensions
{
    extension(DocumentDatabaseFixture fixture)
    {
        public async Task<Guid> RegisterMachineAsync(Guid companyId)
        {
            var message = MachineRegistrationTestData.CreateMessage() with { CompanyId = companyId };

            await fixture.RegisterMachineAsync(message);

            return message.MachineId;
        }

        public async Task RegisterMachineAsync(MachineCreated message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineCreated>>();

            await handler.HandleAsync(message);
        }

        public async Task RenameMachineAsync(MachineRenamed message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineRenamed>>();

            await handler.HandleAsync(message);
        }

        public async Task UnregisterMachineAsync(MachineDeleted message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineDeleted>>();

            await handler.HandleAsync(message);
        }

        public async Task<MachineRegistration?> LoadRegistrationAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .AsNoTracking()
                .SingleOrDefaultAsync(registration => registration.Id == machineId);
        }

        public async Task<int> CountRegistrationsAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<IDocumentDbContext>()
                .MachineRegistrations
                .IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                .CountAsync(registration => registration.Id == machineId);
        }
    }
}
