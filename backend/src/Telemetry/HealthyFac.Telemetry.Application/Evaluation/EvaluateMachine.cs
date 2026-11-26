using FluentValidation;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Domain.Evaluation;
using HealthyFac.Telemetry.Domain.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Application.Evaluation;

public sealed record EvaluateMachineRequest : IRequest
{
    public required Guid MachineId { get; init; }

    public required Guid CompanyId { get; init; }
}

public class EvaluateMachineRequestValidator : AbstractValidator<EvaluateMachineRequest>
{
    public EvaluateMachineRequestValidator()
    {
        RuleFor(request => request.MachineId)
            .NotEmpty()
            .WithMessage(EvaluationErrors.MachineIdRequired);

        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EvaluationErrors.CompanyIdRequired);
    }
}

public sealed class EvaluateMachinePreHandlerCheck : PassThroughPreHandlerCheck<EvaluateMachineRequest>;

/// <summary>
/// Evaluates the new samples of one machine: it follows the phase, learns the statistics, runs the rules, and stores
/// the episodes with their messages. The run holds the claim of the worker, and the machine row carries the row
/// version, so a second worker that took the same machine fails its save.
/// </summary>
/// <remarks>
/// Everything of one run (statistics, streaks, episodes, outbox rows, the phase tracking, the watermark, and the next
/// due time) goes into one save inside <c>RunInTransactionAsync</c>, so a cancelled run stores nothing and the next run
/// reads the same samples again (rule <b>Cancellation Tokens</b>, form <b>Resume</b>).
/// </remarks>
/// <param name="dbContext">Reads the definitions and stores the result.</param>
/// <param name="outbox">Queues the episode messages.</param>
/// <param name="timeProvider">Provides the time of the run.</param>
/// <param name="options">The detection values.</param>
/// <param name="reader">Reads the sample batch.</param>
/// <param name="metrics">Counts the late readings.</param>
/// <param name="fleetSilence">Tells whether the whole fleet is silent.</param>
public sealed class EvaluateMachineRequestHandler(
    ITelemetryDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider,
    IOptions<DetectionOptions> options,
    SampleBatchReader reader,
    DetectionMetrics metrics,
    FleetSilence fleetSilence
) : RequestHandler<EvaluateMachineRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly DetectionOptions _options = options.Value;
    private readonly SampleBatchReader _reader = reader;
    private readonly DetectionMetrics _metrics = metrics;
    private readonly FleetSilence _fleetSilence = fleetSilence;

    protected override async Task HandleInternalAsync(
        EvaluateMachineRequest request,
        CancellationToken cancellationToken
    )
    {
        var lateCount = 0;

        await _dbContext.RunInTransactionAsync(
            async token => { lateCount = await RunAsync(request, token); },
            cancellationToken);

        _metrics.AddLateReadings(lateCount);
    }

    private async Task<int> RunAsync(EvaluateMachineRequest request, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var evaluation = await _dbContext.MachineEvaluations
            .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken);

        var registration = await _dbContext.MachineRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == request.MachineId, cancellationToken);

        if (evaluation is null || registration is null || registration.CompanyId != request.CompanyId)
        {
            throw new NotFoundException(EvaluationErrors.MachineNotFound);
        }

        if (registration.State != MachineRegistrationState.Operational)
        {
            var newest = await _reader.LatestSampledAtAsync(
                registration.Id,
                evaluation.Watermark,
                now.AddSeconds(-_options.SettleLagSeconds),
                cancellationToken);

            var openInterval = await _dbContext.PhaseIntervals
                .Where(interval => interval.MachineRegistrationId == registration.Id && interval.EndedAt == null)
                .SingleOrDefaultAsync(cancellationToken);

            openInterval?.Close(evaluation.Watermark ?? now);
            evaluation.CompleteRun(newest, now, now.AddSeconds(_options.EvaluationIntervalSeconds));
            await _dbContext.SaveChangesAsync(cancellationToken);

            return 0;
        }

        var run = await MachineRun.LoadAsync(
            _dbContext, _outbox, _options, registration, evaluation, cancellationToken);

        var lateCount = await CountLateAsync(evaluation, cancellationToken);
        var maxSamples = Math.Max(val1: 1, _options.CatchUpBatchReadings / Math.Max(val1: 1, run.DefinitionCount));

        var samples = await _reader.ReadAsync(
            registration.Id,
            evaluation.Watermark,
            now.AddSeconds(-_options.SettleLagSeconds),
            maxSamples,
            run.Metrics,
            [.. run.Statuses.Select(status => status.Id)],
            cancellationToken);

        foreach (var sample in samples)
        {
            run.Process(sample);
        }

        await run.RunSilenceAsync(registration, now, _reader, _fleetSilence.IsSilent, cancellationToken);

        evaluation.CompleteRun(
            samples.Count > 0 ? samples[^1].SampledAt : null,
            now,
            now.AddSeconds(_options.EvaluationIntervalSeconds));

        await _dbContext.SaveChangesAsync(cancellationToken);

        return lateCount;
    }

    private async Task<int> CountLateAsync(MachineEvaluation evaluation, CancellationToken cancellationToken) =>
        evaluation is { LastRunAt: { } lastRunAt, Watermark: { } watermark }
            ? await _reader.CountLateAsync(evaluation.Id, lastRunAt, watermark, cancellationToken)
            : 0;
}
