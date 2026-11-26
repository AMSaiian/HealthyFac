using HealthyFac.Core.Application;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Intake;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Application.StatusDefinitions;
using HealthyFac.Telemetry.Domain.Intake;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

/// <summary>
/// Arranges companies, machines, registrations, and definitions through the Core and Telemetry use cases, and drives
/// the loop steps of the simulator host. Every method opens its own DI scope, so it reads what is stored, not tracked
/// objects.
/// </summary>
public static class SimulatorFixtureExtensions
{
    extension(SimulatorFixture fixture)
    {
        public async Task<Guid> CreateCompanyAsync()
        {
            using var scope = fixture.CoreScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ICoreDbContext>();

            var company = new Company
            {
                Name = SimulatorTestData.CompanyName,
                TimeZoneId = SimulatorTestData.CompanyTimeZoneId,
            };

            dbContext.Companies.Add(company);
            await dbContext.SaveChangesAsync();

            return company.Id;
        }

        public async Task<ArrangedMachine> CreateMachineAsync(
            Guid companyId,
            string serialNumber,
            int goLiveInDays = 0
        )
        {
            using var scope = fixture.CreateCoreUserScope(companyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<CreateMachineRequest, CreateMachineResponse>>();

            var zone = TimeZoneInfo.FindSystemTimeZoneById(SimulatorTestData.CompanyTimeZoneId);
            var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, zone).DateTime);

            var response = await handler.HandleAsync(
                new CreateMachineRequest
                {
                    CompanyId = companyId,
                    Name = $"Machine {serialNumber}",
                    SerialNumber = serialNumber,
                    ExpectedGoLiveOn = today.AddDays(goLiveInDays),
                });

            var gatewayId = await fixture.LoadGatewayIdAsync(response.Id);

            return new ArrangedMachine(companyId, response.Id, gatewayId, serialNumber);
        }

        public async Task RegisterMachineAsync(ArrangedMachine machine)
        {
            using var scope = fixture.TelemetryScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineCreated>>();

            await handler.HandleAsync(
                new MachineCreated
                {
                    MachineId = machine.MachineId,
                    GatewayId = machine.GatewayId,
                    CompanyId = machine.CompanyId,
                    Name = $"Machine {machine.SerialNumber}",
                    ExpectedGoLiveOn = SimulatorTestData.GoLiveDay,
                    GoLiveStartsAt = SimulatorTestData.GoLiveStart,
                });
        }

        public async Task UnregisterMachineAsync(ArrangedMachine machine)
        {
            using var scope = fixture.TelemetryScopeFactory.CreateScope();
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<MachineDeleted>>();

            await handler.HandleAsync(
                new MachineDeleted { MachineId = machine.MachineId, CompanyId = machine.CompanyId });
        }

        public async Task DeleteMachineAsync(ArrangedMachine machine)
        {
            using var scope = fixture.CreateCoreUserScope(machine.CompanyId);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<DeleteMachineRequest>>();

            await handler.HandleAsync(
                new DeleteMachineRequest { CompanyId = machine.CompanyId, Id = machine.MachineId });
        }

        public async Task ApplyDefinitionsAsync(ArrangedMachine machine)
        {
            var definitions = SimulatorTestData.ReadDemoDefinitions(machine.SerialNumber);

            using (var scope = fixture.CreateTelemetryUserScope(machine.CompanyId))
            {
                var handler = scope.ServiceProvider.GetRequiredService<
                    IRequestHandler<ApplyMetricDefinitionChangesRequest, ApplyMetricDefinitionChangesResponse>>();

                await handler.HandleAsync(
                    new ApplyMetricDefinitionChangesRequest
                    {
                        CompanyId = machine.CompanyId,
                        MachineId = machine.MachineId,
                        ExpectedRevision = 0,
                        Created = definitions.Metrics,
                    });
            }

            if (definitions.Statuses.Count == 0)
            {
                return;
            }

            using var statusScope = fixture.CreateTelemetryUserScope(machine.CompanyId);

            var statusHandler = statusScope.ServiceProvider.GetRequiredService<
                IRequestHandler<ApplyStatusDefinitionChangesRequest, ApplyStatusDefinitionChangesResponse>>();

            await statusHandler.HandleAsync(
                new ApplyStatusDefinitionChangesRequest
                {
                    CompanyId = machine.CompanyId,
                    MachineId = machine.MachineId,
                    Items =
                    [
                        .. definitions.Statuses.Select(status => new StatusDefinitionChange
                        {
                            Code = status.Code, Severity = status.Severity,
                        }),
                    ],
                });
        }

        public async Task<ArrangedMachine> ArrangeRunningMachineAsync(string serialNumber, bool isDiscovered = true)
        {
            var companyId = await fixture.CreateCompanyAsync();
            var machine = await fixture.CreateMachineAsync(companyId, serialNumber);
            await fixture.RegisterMachineAsync(machine);
            await fixture.ApplyDefinitionsAsync(machine);

            if (isDiscovered)
            {
                await fixture.DiscoverAsync();
            }

            return machine;
        }

        public async Task StoreReadingAsync(Guid machineId, string key, double value)
        {
            using var scope = fixture.TelemetryScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            var metricId = await dbContext.MetricDefinitions
                .Where(metric => metric.MachineRegistrationId == machineId && metric.Key == key)
                .Select(metric => metric.Id)
                .SingleAsync();

            dbContext.NumericReadings.Add(
                new NumericReading(metricId, DateTime.UtcNow.AddMinutes(value: -1), value, isOutOfRange: false));

            await dbContext.SaveChangesAsync();
        }

        public async Task<string> LoadGatewayIdAsync(Guid machineId)
        {
            using var scope = fixture.CoreScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ICoreDbContext>()
                .Machines
                .Where(machine => machine.Id == machineId)
                .Select(machine => machine.GatewayId)
                .SingleAsync();
        }

        public async Task<string> LoadSchemaJsonAsync(ArrangedMachine machine)
        {
            using var scope = fixture.CreateTelemetryUserScope(machine.CompanyId);

            var handler = scope.ServiceProvider
                .GetRequiredService<IRequestHandler<GetMetricSchemaRequest, GetMetricSchemaResponse>>();

            var response = await handler.HandleAsync(
                new GetMetricSchemaRequest { CompanyId = machine.CompanyId, MachineId = machine.MachineId });

            return response.Schema.GetRawText();
        }

        public async Task<IReadOnlyList<IntakeError>> ParseAsync(
            ArrangedMachine machine,
            ReceivedMachineMessage message
        )
        {
            var schemaJson = await fixture.LoadSchemaJsonAsync(machine);
            using var scope = fixture.TelemetryScopeFactory.CreateScope();
            var parser = scope.ServiceProvider.GetRequiredService<IMachineMessageParser>();

            return parser.Parse(schemaJson, message.Text, DateTime.UtcNow).Errors;
        }

        public async Task WriteThroughSimulatorConnectionAsync()
        {
            await using var connection = new NpgsqlConnection(fixture.SimulatorCoreConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(cmdText: "DELETE FROM machines", connection);

            await command.ExecuteNonQueryAsync();
        }

        public async Task DiscoverAsync() => await fixture.Loops.DiscoverAsync(CancellationToken.None);

        public async Task PollStateAsync() => await fixture.Loops.PollStateAsync(CancellationToken.None);

        /// <summary>
        /// Advances the clock by one second for each tick. The publish loop of the host wakes and sends one message
        /// for each second.
        /// </summary>
        public async Task<IReadOnlyList<ReceivedMachineMessage>> TickAsync(int count = 1)
        {
            fixture.Subscriber.Clear();

            for (var tick = 1; tick <= count; tick++)
            {
                fixture.Clock.Advance(TimeSpan.FromSeconds(seconds: 1));
                await fixture.Subscriber.WaitForAsync(tick, SimulatorTestData.WaitLimit);
            }

            return fixture.Subscriber.Messages;
        }

        public async Task<IReadOnlyList<ReceivedMachineMessage>> TickExpectingNothingAsync()
        {
            fixture.Subscriber.Clear();
            fixture.Clock.Advance(TimeSpan.FromSeconds(seconds: 1));
            await fixture.Subscriber.WaitForAsync(count: 1, SimulatorTestData.ShortWait);

            return fixture.Subscriber.Messages;
        }
    }
}

public sealed record ArrangedMachine(Guid CompanyId, Guid MachineId, string GatewayId, string SerialNumber);
