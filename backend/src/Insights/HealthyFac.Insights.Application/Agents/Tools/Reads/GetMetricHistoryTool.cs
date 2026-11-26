using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.Signals;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Agents.Tools.Reads;

/// <summary>
/// The arguments that the model gives <c>get_metric_history</c>. A missing <see cref="To"/> is now, and a missing
/// <see cref="From"/> is the option <see cref="AgentOptions.PlannerHistoryMinutes"/> before <see cref="To"/>.
/// </summary>
public sealed record GetMetricHistoryArguments
{
    public required string MetricKey { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }
}

[AgentTool(role: "planner", ToolName, Description = ToolDescription, IsRead = true)]
[DownstreamScopes("telemetry")]
public sealed record GetMetricHistoryToolRequest
    : IAgentToolRequest<GetMetricHistoryToolRequest, GetMetricHistoryArguments, GetMetricHistoryToolResponse>
{
    public const string ToolName = "get_metric_history";

    public const string ToolDescription =
        "Reads the history of one metric of the machine as buckets with the average, the minimum, the maximum, and the "
        + "sample count. Give the metric by its key (for example pump.vibration); the keys are in the signals of the "
        + "case and in the manual. Without from and to, it reads the most recent minutes.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required string MetricKey { get; init; }

    public DateTime? From { get; init; }

    public DateTime? To { get; init; }

    public static GetMetricHistoryToolRequest Create(GetMetricHistoryArguments arguments, ToolContext context) =>
        new()
        {
            CompanyId = context.CompanyId,
            MachineId = context.RequireMachineId(),
            MetricKey = arguments.MetricKey,
            From = arguments.From,
            To = arguments.To,
        };
}

public sealed record GetMetricHistoryToolResponse
{
    public required DateTime From { get; init; }

    public required DateTime To { get; init; }

    public required IReadOnlyList<MetricHistoryBucket> Buckets { get; init; }
}

public class GetMetricHistoryToolRequestValidator : AbstractValidator<GetMetricHistoryToolRequest>
{
    public GetMetricHistoryToolRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);

        RuleFor(request => request.MetricKey)
            .NotEmpty()
            .SingleLineText()
            .WithMessage(AgentMessages.MetricKeyRequired);
    }
}

public sealed class GetMetricHistoryToolPreHandlerCheck(IInsightsDbContext dbContext, ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetMetricHistoryToolRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetMetricHistoryToolRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Reads the history of one metric from Telemetry in <see cref="AgentOptions.PlannerHistoryBucketCount"/> buckets. The
/// metric key names an active metric definition of the machine of the case (the catalog of the machine gives its ID);
/// an unknown key is rejected to the model. A range above <see cref="AgentOptions.HistoryMaxRangeHours"/> hours,
/// and a range whose start is not before its end,
/// is rejected to the model.
/// </summary>
/// <param name="signals">Reads the history.</param>
/// <param name="catalog">Reads the active metric definitions of the machine.</param>
/// <param name="options">Holds the default range, the bucket count, and the range limit.</param>
/// <param name="timeProvider">Provides now.</param>
public sealed class GetMetricHistoryToolRequestHandler(
    ITelemetrySignals signals,
    IMachineSignalCatalog catalog,
    IOptions<AgentOptions> options,
    TimeProvider timeProvider
) : IRequestHandler<GetMetricHistoryToolRequest, GetMetricHistoryToolResponse>
{
    private readonly ITelemetrySignals _signals = signals;
    private readonly IMachineSignalCatalog _catalog = catalog;
    private readonly AgentOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<GetMetricHistoryToolResponse> HandleAsync(
        GetMetricHistoryToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var to = request.To ?? _timeProvider.GetUtcNow().UtcDateTime;
        var from = request.From ?? to.AddMinutes(-_options.PlannerHistoryMinutes);

        if (from >= to)
        {
            throw new UnprocessableException(AgentMessages.RangeStartNotBeforeEnd);
        }

        if (to - from > TimeSpan.FromHours(_options.HistoryMaxRangeHours))
        {
            throw new UnprocessableException(AgentMessages.HistoryRangeTooLong(_options.HistoryMaxRangeHours));
        }

        var machineSignals = await _catalog.GetAsync(request.MachineId, cancellationToken);

        var metric = machineSignals.Metrics.FirstOrDefault(candidate => candidate.Key == request.MetricKey)
            ?? throw new UnprocessableException(AgentMessages.UnknownMetricKey);

        var buckets = await _signals.GetMetricHistoryAsync(
            request.MachineId,
            metric.Id,
            from,
            to,
            _options.PlannerHistoryBucketCount,
            cancellationToken);

        return new GetMetricHistoryToolResponse { From = from, To = to, Buckets = buckets };
    }
}
