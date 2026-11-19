using System.Text.Json;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Telemetry.Application;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Domain.Evaluation;
using HealthyFac.Telemetry.Domain.Evaluation.Phases;
using HealthyFac.Telemetry.Domain.Evaluation.Statistics;
using HealthyFac.Telemetry.Domain.Intake;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.OperatingPhases;
using HealthyFac.Telemetry.Domain.SignalEpisodes;
using HealthyFac.Telemetry.Domain.StatusDefinitions;
using HealthyFac.Telemetry.Infrastructure.Database;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.OperatingPhases.Data;
using HealthyFac.Telemetry.Messaging.Signals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Telemetry.IntegrationTests.Evaluation.Data;

/// <summary>
/// Arranges machines, readings, phases, and episodes for the evaluation tests, runs the evaluation, and reads the
/// result back. Every method opens its own DI scope, so it reads what is stored, not tracked objects.
/// </summary>
public static class EvaluationFixtureExtensions
{
    extension(TelemetryEvaluationFixture fixture)
    {
        public async Task<EvaluationArrangement> ArrangeEvaluationMachineAsync(MetricLimits? limits = null)
        {
            var machineId = await fixture.Database.RegisterTenantMachineAsync(MachineRegistrationTestData.CompanyId);
            var metricId = await fixture.Database.AddMetricDefinitionAsync(machineId);
            await fixture.SetLimitsAsync(metricId, limits ?? EvaluationTestData.DefaultLimits);

            return new EvaluationArrangement(
                machineId,
                metricId,
                await fixture.AddStatusAsync(machineId, code: "cutting", StatusSeverity.Info),
                await fixture.AddStatusAsync(machineId, code: "part.change", StatusSeverity.Info),
                await fixture.AddStatusAsync(machineId, code: "lowCoolant", StatusSeverity.Warning),
                await fixture.AddStatusAsync(machineId, code: "spindleFault", StatusSeverity.Error));
        }

        public async Task SetLimitsAsync(Guid metricId, MetricLimits limits)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var metric = await dbContext.MetricDefinitions.SingleAsync(candidate => candidate.Id == metricId);

            metric.ChangeLimits(limits, MetricBehavior.Level);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> AddStatusAsync(Guid machineId, string code, StatusSeverity severity)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var status = new StatusDefinition(machineId, code, severity, description: null);

            dbContext.StatusDefinitions.Add(status);
            await dbContext.SaveChangesAsync();

            return status.Id;
        }

        public async Task<Guid> AddEvaluationPhaseAsync(
            EvaluationArrangement arrangement,
            string code,
            int priority,
            Guid statusId,
            bool isTracked,
            MetricLimits? limits
        )
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var condition = OperatingPhaseCondition.Create(
                PhaseConditionSource.Status(statusId),
                PhaseConditionOperator.Active,
                value: null,
                upperValue: null,
                states: null);
            var phase = new OperatingPhase(
                arrangement.MachineId,
                code,
                code,
                description: null,
                priority,
                isTracked,
                [condition],
                limits is null
                    ? []
                    : [OperatingPhaseLimit.Create(arrangement.MetricId, MetricValueKind.Numeric, limits)]);

            dbContext.OperatingPhases.Add(phase);
            await dbContext.SaveChangesAsync();

            return phase.Id;
        }

        public async Task SeedSamplesAsync(EvaluationArrangement arrangement, IEnumerable<SeededSample> samples)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            foreach (var sample in samples)
            {
                dbContext.ReceivedMessages.Add(
                    new ReceivedMessage(
                        arrangement.MachineId,
                        EvaluationTestData.Topic,
                        sample.SampledAt,
                        sample.ReceivedAt ?? sample.SampledAt,
                        schemaRevision: 0,
                        rawPayload: null,
                        payload: "{}",
                        []));

                foreach (var (metricId, value) in sample.Values)
                {
                    dbContext.NumericReadings.Add(
                        new NumericReading(metricId, sample.SampledAt, value, isOutOfRange: false));
                }

                foreach (var statusId in sample.ActiveStatusIds)
                {
                    dbContext.StatusReadings.Add(new StatusReading(statusId, sample.SampledAt));
                }
            }

            await dbContext.SaveChangesAsync();
        }

        public async Task EvaluateAsync(Guid machineId)
        {
            using var scope = fixture.CreateActorScope(TelemetryServiceActors.Evaluator);
            var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<EvaluateMachineRequest>>();

            await handler.HandleAsync(
                new EvaluateMachineRequest
                {
                    MachineId = machineId,
                    CompanyId = MachineRegistrationTestData.CompanyId,
                });
        }

        public async Task<MachineEvaluation> LoadEvaluationAsync(Guid machineId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MachineEvaluations
                .AsNoTracking()
                .SingleAsync(evaluation => evaluation.Id == machineId);
        }

        public async Task<List<PhaseInterval>> LoadPhaseIntervalsAsync(Guid machineId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .PhaseIntervals
                .AsNoTracking()
                .Where(interval => interval.MachineRegistrationId == machineId)
                .OrderBy(interval => interval.StartedAt)
                .ToListAsync();
        }

        public async Task AddPhaseIntervalAsync(Guid machineId, Guid? phaseId, DateTime startedAt, DateTime? endedAt)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var interval = new PhaseInterval(machineId, phaseId, startedAt);

            if (endedAt is { } end)
            {
                interval.Close(end);
            }

            dbContext.PhaseIntervals.Add(interval);
            await dbContext.SaveChangesAsync();
        }

        public async Task<List<MetricStatistics>> LoadStatisticsAsync(Guid metricId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .MetricStatistics
                .AsNoTracking()
                .Where(row => row.MetricDefinitionId == metricId)
                .OrderBy(row => row.Id)
                .ToListAsync();
        }

        public async Task<List<SignalEpisode>> LoadEpisodesAsync(Guid machineId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>()
                .SignalEpisodes
                .AsNoTracking()
                .Where(episode => episode.MachineRegistrationId == machineId)
                .OrderBy(episode => episode.OpenedAt)
                .ThenBy(episode => episode.Id)
                .ToListAsync();
        }

        public async Task<List<EpisodeMessage>> LoadEpisodeMessagesAsync(Guid machineId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
            var payloads = await dbContext.OutboxMessages
                .AsNoTracking()
                .Where(message => message.Type == SignalEpisodeChanged.MessageType)
                .OrderBy(message => message.Id)
                .Select(message => message.Payload)
                .ToListAsync();

            return payloads
                .Select(payload => JsonSerializer.Deserialize<EpisodeMessage>(payload, EvaluationTestData.Json)!)
                .Where(message => message.MachineId == machineId)
                .OrderBy(message => message.ChangedAt)
                .ThenBy(message => message.Sequence)
                .ToList();
        }

        public async Task<int> CountOutboxEpisodeMessagesAsync()
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();

            return await scope.ServiceProvider.GetRequiredService<TelemetryDbContext>()
                .OutboxMessages
                .CountAsync(message => message.Type == SignalEpisodeChanged.MessageType);
        }

        public async Task AddEpisodeAsync(
            PhaseArrangement arrangement,
            SignalRule rule,
            Guid? metricId,
            Guid? statusId
        )
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            dbContext.SignalEpisodes.Add(OpenEpisode(arrangement.MachineId, rule, metricId, statusId));
            await dbContext.SaveChangesAsync();
        }

        public async Task AddEpisodeAtAsync(
            EvaluationArrangement arrangement,
            SignalRule rule,
            DateTime openedAt,
            DateTime? resolvedAt
        )
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var episode = SignalEpisode.Open(
                arrangement.MachineId,
                EvaluationTestData.CompanyId,
                rule,
                arrangement.MetricId,
                statusDefinitionId: null,
                SignalSeverity.Warning,
                openedAt,
                operatingPhaseId: null,
                new SignalSnapshot { Value = 1, Limit = 9 });

            if (resolvedAt is { } resolved)
            {
                episode.Resolve(resolved, new SignalSnapshot { Value = 1, Limit = 9 });
            }

            dbContext.SignalEpisodes.Add(episode);
            await dbContext.SaveChangesAsync();
        }

        public async Task<Guid> AddResolvableEpisodeAsync(
            PhaseArrangement arrangement,
            SignalRule rule,
            Guid? metricId,
            Guid? statusId
        )
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var episode = OpenEpisode(arrangement.MachineId, rule, metricId, statusId);

            dbContext.SignalEpisodes.Add(episode);
            await dbContext.SaveChangesAsync();

            return episode.Id;
        }

        public async Task ResolveEpisodeAsync(Guid episodeId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();
            var episode = await dbContext.SignalEpisodes.SingleAsync(candidate => candidate.Id == episodeId);

            episode.Resolve(EvaluationTestData.Start.AddMinutes(value: 1), new SignalSnapshot { Value = 0 });
            await dbContext.SaveChangesAsync();
        }

        public async Task AddStatisticsAsync(Guid metricId, Guid? phaseId)
        {
            using var scope = fixture.Database.ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITelemetryDbContext>();

            dbContext.MetricStatistics.Add(new MetricStatistics(metricId, phaseId));
            await dbContext.SaveChangesAsync();
        }
    }

    private static SignalEpisode OpenEpisode(Guid machineId, SignalRule rule, Guid? metricId, Guid? statusId) =>
        SignalEpisode.Open(
            machineId,
            EvaluationTestData.CompanyId,
            rule,
            metricId,
            statusId,
            SignalSeverity.Warning,
            EvaluationTestData.Start,
            operatingPhaseId: null,
            new SignalSnapshot { Value = 1 });
}

public sealed record EvaluationArrangement(
    Guid MachineId,
    Guid MetricId,
    Guid CuttingStatusId,
    Guid PartChangeStatusId,
    Guid WarningStatusId,
    Guid ErrorStatusId
);

public sealed record SeededSample(
    DateTime SampledAt,
    IReadOnlyDictionary<Guid, double> Values,
    IReadOnlyList<Guid> ActiveStatusIds,
    DateTime? ReceivedAt = null
);

public sealed record EpisodeMessage
{
    public Guid EpisodeId { get; init; }

    public Guid MachineId { get; init; }

    public string Rule { get; init; } = string.Empty;

    public string Severity { get; init; } = string.Empty;

    public string State { get; init; } = string.Empty;

    public string? MetricKey { get; init; }

    public string? StatusCode { get; init; }

    public string? PhaseCode { get; init; }

    public DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }

    public int Sequence { get; init; }

    public DateTime ChangedAt { get; init; }
}
