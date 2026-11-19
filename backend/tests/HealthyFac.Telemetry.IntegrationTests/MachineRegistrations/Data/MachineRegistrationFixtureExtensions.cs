using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

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
            new(Guid.CreateVersion7());

        public async Task<Guid> RegisterTenantMachineAsync(Guid companyId)
        {
            var message = MachineRegistrationTestData.CreateMessage() with
            {
                CompanyId = companyId,
            };

            await fixture.RegisterMachineAsync(message);

            return message.MachineId;
        }

        /// <summary>
        /// Runs <paramref name="holdingMessage"/> in one open transaction, so the registration lock stays held. It then
        /// starts <paramref name="waitingMessage"/> on another connection, waits until a session of the database waits
        /// for a lock, and commits the first transaction.
        /// </summary>
        /// <param name="holdingMessage">The handler call that takes the lock first.</param>
        /// <param name="waitingMessage">The handler call that must wait for the lock.</param>
        /// <returns>A task that completes when both calls ended.</returns>
        public async Task RunWithRegistrationLockHeldAsync(
            Func<IServiceProvider, Task> holdingMessage,
            Func<Task> waitingMessage
        )
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var holdingTask = new TaskCompletionSource();
            Task? waiting = null;

            var holding = dbContext.RunInTransactionAsync(
                async token =>
                {
                    await holdingMessage(scope.ServiceProvider);
                    holdingTask.SetResult();
                    await fixture.WaitForLockWaitAsync(token);
                },
                TestContext.Current.CancellationToken);

            await holdingTask.Task;
            waiting = waitingMessage();

            await Task.WhenAll(holding, waiting);
        }

        /// <summary>Waits up to 10 seconds until a session waits for a lock; returns when none did.</summary>
        public async Task WaitForLockWaitAsync(CancellationToken cancellationToken)
        {
            await using var connection = new NpgsqlConnection(fixture.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new NpgsqlCommand(
                cmdText: "SELECT count(*) FROM pg_stat_activity WHERE wait_event_type = 'Lock'", connection);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(seconds: 10));

            try
            {
                while (Convert.ToInt32(await command.ExecuteScalarAsync(timeout.Token)) == 0)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(milliseconds: 50), timeout.Token);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
            }
        }

        public Task DeleteMachineAsync(Guid companyId, Guid machineId) =>
            fixture.UnregisterMachineAsync(new MachineDeleted { MachineId = machineId, CompanyId = companyId });

        public async Task RegisterMachineAsync(MachineCreated message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineCreated>>();

            await handler.HandleAsync(message);
        }

        public async Task ChangeMachineStateAsync(MachineStateChanged message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineStateChanged>>();

            await handler.HandleAsync(message);
        }

        public async Task ChangeMachineGoLiveDateAsync(MachineGoLiveDateChanged message)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineGoLiveDateChanged>>();

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

        public async Task<Guid> StoreMetricDraftOnMachineAsync(Guid companyId, Guid machineId, bool isApplied)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var entry = new ProposalEntry(Guid.CreateVersion7(), companyId);
            var draft = DefinitionDraftTestData.CreateMetric(Guid.CreateVersion7(), entry.Id, machineId, companyId);

            entry.AcceptOnMachine(machineId);
            draft.Follow(entry, isTargetRegistrationDeleted: false);

            if (isApplied)
            {
                draft.MarkApplied();
            }

            dbContext.ProposalEntries.Add(entry);
            dbContext.MetricDefinitionDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }

        public async Task<Guid> StoreAwaitingStatusDraftOnMachineAsync(Guid companyId, Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var entry = new ProposalEntry(Guid.CreateVersion7(), companyId);
            var draft = DefinitionDraftTestData.CreateStatus(Guid.CreateVersion7(), entry.Id, machineId, companyId);

            dbContext.ProposalEntries.Add(entry);
            dbContext.StatusDefinitionDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
        }

        public async Task<Guid> StorePendingStatusDraftOnMachineAsync(Guid companyId, Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var entry = new ProposalEntry(Guid.CreateVersion7(), companyId);
            var draft = DefinitionDraftTestData.CreateStatus(Guid.CreateVersion7(), entry.Id, machineId, companyId);

            entry.AcceptOnMachine(machineId);
            draft.Follow(entry, isTargetRegistrationDeleted: false);
            dbContext.ProposalEntries.Add(entry);
            dbContext.StatusDefinitionDrafts.Add(draft);
            await dbContext.SaveChangesAsync();

            return draft.Id;
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

public sealed record MachineTenant(Guid CompanyId);
