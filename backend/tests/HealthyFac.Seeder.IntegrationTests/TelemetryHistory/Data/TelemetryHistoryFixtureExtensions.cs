using System.Text.Json;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Agents.Tools.Writes;
using HealthyFac.Insights.Application.Cases;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.Domain.Cases;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Insights.Infrastructure.Database;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Seeder.IntegrationTests.Hosting;
using HealthyFac.Seeder.IntegrationTests.People.Data;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Telemetry.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

public static class TelemetryHistoryFixtureExtensions
{
    extension(SeederFixture fixture)
    {
        public async Task<Dictionary<string, Guid>> LoadMachineIdsByKeyAsync()
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            var machines = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .Machines
                .AsNoTracking()
                .ToListAsync();

            return new Dictionary<string, Guid>
            {
                [TelemetryHistorySeedTestData.Pump] =
                    machines.Single(machine => machine.SerialNumber == ScenarioBinding.PumpSerialNumber).Id,
                [TelemetryHistorySeedTestData.Lathe] =
                    machines.Single(machine => machine.SerialNumber == ScenarioBinding.CncSerialNumber).Id,
                [TelemetryHistorySeedTestData.Compressor] = machines
                    .Single(machine => machine.SerialNumber == ScenarioBinding.CompressorSerialNumber)
                    .Id,
            };
        }

        public async Task<List<StateRow>> LoadStateRowsAsync(Guid machineId)
        {
            using var scope = fixture.Hosts.Core.CreateScope(SeederTestData.InitializationActor);

            var rows = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .MachineStateHistories
                .AsNoTracking()
                .Where(row => row.MachineId == machineId)
                .OrderBy(row => row.StartedAt)
                .ToListAsync();

            return [.. rows.Select(row => new StateRow { State = row.State.Value, StartedAt = row.StartedAt })];
        }

        public async Task<Dictionary<Guid, int>> CountMessagesByMachineAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            var counts = await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking()
                .GroupBy(message => message.MachineRegistrationId)
                .Select(group => new { MachineId = group.Key, Count = group.Count() })
                .ToListAsync();

            return counts.ToDictionary(entry => entry.MachineId!.Value, entry => entry.Count);
        }

        public async Task<int> CountMessagesInRangeAsync(
            Guid machineId,
            DateTime fromInclusive,
            DateTime untilExclusive
        )
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking()
                .CountAsync(message => message.MachineRegistrationId == machineId
                    && message.SampledAt >= fromInclusive
                    && message.SampledAt < untilExclusive);
        }

        public async Task<TimeSpan> ReadEvaluationStepAsync()
        {
            var definitionCount = await fixture.CountLargestDefinitionSetAsync();

            return TimeSpan.FromMinutes(
                TelemetryHistoryPart.StepMinutes(
                    TelemetryHistorySeedTestData.SampleIntervalSeconds,
                    TelemetryHistorySeedTestData.CatchUpBatchReadings,
                    definitionCount));
        }

        public async Task<int> CountLargestDefinitionSetAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            var metrics = await dbContext.MetricDefinitions
                .AsNoTracking()
                .GroupBy(metric => metric.MachineRegistrationId)
                .Select(group => new { MachineId = group.Key, Count = group.Count() })
                .ToListAsync();

            var statuses = await dbContext.StatusDefinitions
                .AsNoTracking()
                .GroupBy(status => status.MachineRegistrationId)
                .Select(group => new { MachineId = group.Key, Count = group.Count() })
                .ToListAsync();

            return metrics.Max(metric => metric.Count
                + statuses.Where(status => status.MachineId == metric.MachineId).Sum(status => status.Count));
        }

        public async Task<(int WithErrors, int WithoutSchemaOrPayload)> CountBrokenMessagesAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            var messages = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking();

            var withErrors = await messages.CountAsync(message => message.Errors.Count > 0);

            var incomplete =
                await messages.CountAsync(message => message.SchemaRevision == null || message.Payload == null);

            return (withErrors, incomplete);
        }

        public async Task<(DateTime First, DateTime Last)> FindSampleRangeAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            var times = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking()
                .Select(message => message.SampledAt!.Value);

            return (await times.MinAsync(), await times.MaxAsync());
        }

        public async Task<bool> HasReceivedMessageAsync(Guid id)
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .ReceivedMessages
                .AsNoTracking()
                .AnyAsync(message => message.Id == id);
        }

        public async Task<List<double>> LoadNumericValuesAsync(
            Guid machineId,
            string metricKey,
            DateTime fromInclusive,
            DateTime untilExclusive
        )
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            var metricId = await dbContext.MetricDefinitions
                .AsNoTracking()
                .Where(metric => metric.MachineRegistrationId == machineId && metric.Key == metricKey)
                .Select(metric => metric.Id)
                .SingleAsync();

            return await dbContext.NumericReadings
                .AsNoTracking()
                .Where(reading => reading.MetricDefinitionId == metricId
                    && reading.SampledAt >= fromInclusive
                    && reading.SampledAt < untilExclusive)
                .Select(reading => reading.Value)
                .ToListAsync();
        }

        public async Task<List<EpisodeRow>> LoadEpisodesAsync()
        {
            using var scope = fixture.Hosts.Telemetry.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();

            var rows = await dbContext.SignalEpisodes
                .AsNoTracking()
                .OrderBy(episode => episode.OpenedAt)
                .ToListAsync();

            var metricKeys = await dbContext.MetricDefinitions
                .AsNoTracking()
                .ToDictionaryAsync(metric => metric.Id, metric => metric.Key);

            return
            [
                .. rows
                    .Select(episode => new EpisodeRow
                    {
                        MachineId = episode.MachineRegistrationId,
                        Rule = episode.Rule.Value,
                        MetricKey = episode.MetricDefinitionId is null
                            ? null
                            : metricKeys[episode.MetricDefinitionId.Value],
                        Severity = episode.Severity.Value,
                        State = episode.State.Value,
                        OpenedAt = episode.OpenedAt,
                        ResolvedAt = episode.ResolvedAt,
                    }),
            ];
        }

        public async Task<IReadOnlyList<SeedCheckFailure>> CheckTelemetrySeedAsync(
            string baseDirectory,
            DateTimeOffset anchor,
            CancellationToken cancellationToken
        )
        {
            var folder = new SeedFolder(Path.Combine(baseDirectory, PeopleTestData.SeedFolder));

            var events = SeedPlan.Load(
                SeedParts.Create(fixture.Hosts, SeedRunFixtureExtensions.People(fixture), isTelemetry: true), folder);

            var check = new SeedCheck(
                anchor,
                TimeZoneInfo.FindSystemTimeZoneById(PeopleTestData.ZoneId),
                (request, token) => fixture.Hosts.All.Single(host => host.Name == request.Service)
                    .ValidateAsync(request.Request, token),
                SeedCompletion.FindMarkerItem(folder),
                new TelemetryScript
                {
                    Telemetry = folder.Read<TelemetryFile>(TelemetryHistoryConstants.FileName),
                    Machines = folder.Read<MachinesFile>(MachinesConstants.FileName),
                });

            return await check.RunAsync([.. events.Select(planned => planned.Event)], cancellationToken);
        }

        public async Task<IReadOnlyList<CaseToolCallResponse>> LoadToolCallsAsync(
            Guid caseId,
            Guid runId,
            CancellationToken cancellationToken
        )
        {
            var response = await fixture.Hosts.Insights.SendAsync<GetRunToolCallsRequest, CaseToolCallsResponse>(
                ServiceActor.DatabaseInitialization.For(ServiceName.Insights, PeopleTestData.CompanyId),
                new GetRunToolCallsRequest { CompanyId = PeopleTestData.CompanyId, CaseId = caseId, RunId = runId },
                cancellationToken);

            return response.ToolCalls;
        }

        public LinkWorkOrderArguments DecodeLinkArguments(string arguments) =>
            JsonSerializer.Deserialize<LinkWorkOrderArguments>(
                arguments, fixture.Hosts.Insights.GetService<IAgentJson>().Tools)!;

        public async Task<CaseOutcomeResponse> LoadCaseOutcomeAsync(Guid caseId, CancellationToken cancellationToken)
        {
            var detail = await fixture.Hosts.Insights.SendAsync<GetCaseRequest, CaseDetailResponse>(
                ServiceActor.DatabaseInitialization.For(ServiceName.Insights, PeopleTestData.CompanyId),
                new GetCaseRequest { CompanyId = PeopleTestData.CompanyId, Id = caseId },
                cancellationToken);

            return detail.Outcome;
        }

        public async Task<List<AgentToolCall>> LoadAllToolCallsAsync()
        {
            using var scope = fixture.Hosts.Insights.CreateScope(SeederTestData.InitializationActor);

            return await scope.ServiceProvider.GetRequiredService<InsightsDbContext>()
                .AgentToolCalls.AsNoTracking()
                .ToListAsync();
        }

        public async Task<(List<AgentRun> Runs, List<AgentAction> Actions)> LoadAgentRunsAsync()
        {
            using var scope = fixture.Hosts.Insights.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return (
                await dbContext.AgentRuns.AsNoTracking().ToListAsync(),
                await dbContext.AgentActions.AsNoTracking().ToListAsync());
        }

        public async Task<(List<MaintenanceCase> Cases, List<MachineSignal> Signals)> LoadCasesAsync()
        {
            using var scope = fixture.Hosts.Insights.CreateScope(SeederTestData.InitializationActor);
            var dbContext = scope.ServiceProvider.GetRequiredService<InsightsDbContext>();

            return (
                await dbContext.MaintenanceCases.AsNoTracking()
                    .OrderBy(maintenanceCase => maintenanceCase.OpenedAt)
                    .ToListAsync(),
                await dbContext.MachineSignals.AsNoTracking().ToListAsync());
        }
    }
}
