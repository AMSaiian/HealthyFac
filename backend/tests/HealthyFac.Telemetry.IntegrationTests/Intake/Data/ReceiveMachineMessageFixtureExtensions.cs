using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Intake.Data;

/// <summary>
/// Arranges a machine with metrics and statuses, runs the intake use case, and reads what the intake stored.
/// </summary>
public static class ReceiveMachineMessageFixtureExtensions
{
    extension(TelemetryDatabaseFixture fixture)
    {
        public async Task<ArrangedIntakeMachine> ArrangeIntakeMachineAsync(bool isWithDefinitions = true)
        {
            var tenant = fixture.ArrangeMachineTenant();
            var machineId = await fixture.RegisterMachineInDepartmentAsync(tenant.CompanyId, tenant.FirstDepartmentId);
            var registration = await fixture.LoadRegistrationAsync(machineId);

            if (isWithDefinitions)
            {
                await fixture.ApplyMetricChangesAsync(
                    tenant,
                    machineId,
                    expectedRevision: 0,
                    [
                        MetricDefinitionTestData.NewNumeric(ReceiveMachineMessageTestData.TemperatureKey),
                        MetricDefinitionTestData.NewBoolean(ReceiveMachineMessageTestData.DoorKey),
                        MetricDefinitionTestData.NewStateSet(ReceiveMachineMessageTestData.ModeKey),
                    ]);
                await fixture.CreateStatusDefinitionAsync(
                    tenant.CompanyId,
                    tenant.BothDepartmentIds,
                    machineId,
                    ReceiveMachineMessageTestData.ErrorStatus,
                    severity: StatusSeverity.Error);
                await fixture.CreateStatusDefinitionAsync(
                    tenant.CompanyId,
                    tenant.BothDepartmentIds,
                    machineId,
                    ReceiveMachineMessageTestData.WarningStatus,
                    severity: StatusSeverity.Warning);
            }

            return new ArrangedIntakeMachine(machineId, registration!.GatewayId);
        }

        public async Task ReceiveAsync(string topic, byte[] payload, DateTime? receivedAt = null)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<ReceiveMachineMessageRequest>>();

            await handler.HandleAsync(
                new ReceiveMachineMessageRequest
                {
                    Topic = topic,
                    Payload = payload,
                    ReceivedAt = receivedAt ?? ReceiveMachineMessageTestData.ReceivedAt,
                });
        }

        public async Task<IReadOnlyList<ReceivedMessage>> LoadReceivedMessagesAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking()
                .Where(message => message.MachineRegistrationId == machineId)
                .OrderBy(message => message.ReceivedAt)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<ReceivedMessage>> LoadReceivedMessagesByTopicAsync(string topic)
        {
            using var scope = fixture.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking()
                .Where(message => message.Topic == topic)
                .ToListAsync();
        }

        public async Task<StoredReadings> LoadReadingsAsync(Guid machineId)
        {
            using var scope = fixture.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            return new StoredReadings
            {
                Numeric = await dbContext.NumericReadings
                    .AsNoTracking()
                    .Where(reading => dbContext.MetricDefinitions
                        .IgnoreQueryFilters(new[] { SoftDeleteConstants.QueryFilterName })
                        .Any(metric => metric.Id == reading.MetricDefinitionId
                            && metric.MachineRegistrationId == machineId))
                    .ToListAsync(),
                Discrete = await dbContext.DiscreteReadings
                    .AsNoTracking()
                    .Where(reading => dbContext.MetricDefinitions
                        .IgnoreQueryFilters(new[] { SoftDeleteConstants.QueryFilterName })
                        .Any(metric => metric.Id == reading.MetricDefinitionId
                            && metric.MachineRegistrationId == machineId))
                    .ToListAsync(),
                Statuses = await dbContext.StatusReadings
                    .AsNoTracking()
                    .Where(reading => dbContext.StatusDefinitions
                        .IgnoreQueryFilters(new[] { SoftDeleteConstants.QueryFilterName })
                        .Any(status => status.Id == reading.StatusDefinitionId
                            && status.MachineRegistrationId == machineId))
                    .ToListAsync(),
            };
        }
    }
}

public sealed record ArrangedIntakeMachine(Guid MachineId, string GatewayId);

public sealed record StoredReadings
{
    public required IReadOnlyList<NumericReading> Numeric { get; init; }
    public required IReadOnlyList<DiscreteReading> Discrete { get; init; }
    public required IReadOnlyList<StatusReading> Statuses { get; init; }

    public int Total => Numeric.Count + Discrete.Count + Statuses.Count;
}
