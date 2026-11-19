using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Telemetry.Application.Evaluation;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HealthyFac.Telemetry.Application.SignalEpisodes;

public record GetMetricHistoryRequest : IRequest<MetricHistoryResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required Guid MetricDefinitionId { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public required int BucketCount { get; init; }
}

public record MetricHistoryResponse
{
    public required IReadOnlyList<MetricBucketResponse> Buckets { get; init; }
}

/// <summary>
/// One non-empty bucket of the readings of a metric.
/// </summary>
public record MetricBucketResponse
{
    public required DateTime Start { get; init; }

    public required double Avg { get; init; }

    public required double Min { get; init; }

    public required double Max { get; init; }

    public required long Count { get; init; }
}

public class GetMetricHistoryRequestValidator : AbstractValidator<GetMetricHistoryRequest>
{
    public GetMetricHistoryRequestValidator(IOptions<DetectionOptions> options)
    {
        var cap = options.Value.HistoryBucketCap;

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(SignalReadErrors.CompanyIdRequired);

        RuleFor(x => x.MachineId)
            .NotEmpty()
            .WithMessage(SignalReadErrors.MachineIdRequired);

        RuleFor(x => x.MetricDefinitionId)
            .NotEmpty()
            .WithMessage(SignalReadErrors.MetricIdRequired);

        RuleFor(x => x.To)
            .GreaterThan(x => x.From)
            .WithMessage(SignalReadErrors.RangeInvalid);

        RuleFor(x => x.BucketCount)
            .InclusiveBetween(from: 1, cap)
            .WithMessage(SignalReadErrors.BucketCountOutOfRange(cap));
    }
}

/// <summary>
/// Checks the company, then the machine scope: a machine outside the company, and a deleted machine, are "not found"
/// (404).
/// </summary>
/// <param name="dbContext">Reads the machine.</param>
/// <param name="currentUser">Provides the company of the actor.</param>
public sealed class GetMetricHistoryPreHandlerCheck(ITelemetryDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetMetricHistoryRequest>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task CheckAsync(GetMetricHistoryRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        await _dbContext.EnsureMachineInScopeAsync(request.CompanyId, request.MachineId, cancellationToken);
    }
}

/// <summary>
/// Reads the numeric readings of a metric as buckets of equal width, so a long range gives a short answer. The bucket
/// width is the range divided by the bucket count, in whole seconds and at least 1 second, and the buckets start at the
/// start of the range. The answer holds only the non-empty buckets, in time order. A metric of another machine is "not
/// found".
/// </summary>
/// <param name="dbContext">Reads the metric and its readings.</param>
public sealed class GetMetricHistoryRequestHandler(ITelemetryDbContext dbContext)
    : IRequestHandler<GetMetricHistoryRequest, MetricHistoryResponse>
{
    private readonly ITelemetryDbContext _dbContext = dbContext;

    public async Task<MetricHistoryResponse> HandleAsync(
        GetMetricHistoryRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var isMetricOfMachine = await _dbContext.MetricDefinitions.AnyAsync(
            metric => metric.Id == request.MetricDefinitionId && metric.MachineRegistrationId == request.MachineId,
            cancellationToken);

        if (!isMetricOfMachine)
        {
            throw new NotFoundException(SignalReadErrors.MetricNotFound);
        }

        var from = request.From;
        var to = request.To;
        var sizeSeconds = Math.Max(val1: 1.0, Math.Floor((to - from).TotalSeconds / request.BucketCount));

        var rows = await _dbContext.NumericReadings
            .AsNoTracking()
            .Where(reading => reading.MetricDefinitionId == request.MetricDefinitionId
                && reading.SampledAt >= from
                && reading.SampledAt < to)
            .GroupBy(reading => Math.Floor((reading.SampledAt - from).TotalSeconds / sizeSeconds))
            .Select(bucket => new
            {
                Index = bucket.Key,
                Avg = bucket.Average(reading => reading.Value),
                Min = bucket.Min(reading => reading.Value),
                Max = bucket.Max(reading => reading.Value),
                Count = bucket.LongCount(),
            })
            .OrderBy(bucket => bucket.Index)
            .ToListAsync(cancellationToken);

        return new MetricHistoryResponse
        {
            Buckets = rows
                .Select(row => new MetricBucketResponse
                {
                    Start = from.AddSeconds(row.Index * sizeSeconds),
                    Avg = row.Avg,
                    Min = row.Min,
                    Max = row.Max,
                    Count = row.Count,
                })
                .ToList(),
        };
    }
}
