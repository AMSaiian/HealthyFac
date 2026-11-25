using FluentValidation;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

/// <summary>The arguments that the model gives <c>get_metric_history</c>.</summary>
public sealed record GetMetricHistoryArguments
{
    public required Guid MachineId { get; init; }

    public required Guid MetricDefinitionId { get; init; }

    public required DateTime From { get; init; }

    public required DateTime To { get; init; }
}

[AgentTool(
    role: "chat",
    ToolName,
    Description = ToolDescription,
    IsRead = true,
    Permissions = [ChatToolPermissions.Telemetry.SignalsRead],
    Activity = "signals")]
public sealed record GetMetricHistoryToolRequest
    : IAgentToolRequest<GetMetricHistoryToolRequest, GetMetricHistoryArguments, GetMetricHistoryResponse>
{
    public const string ToolName = "get_machine_metric_history";

    public const string ToolDescription =
        "Reads the history of one metric of a machine as buckets with the average, the minimum, the maximum, and "
        + "the sample count. Give the metric by its ID (get_machine lists the metric definitions of a machine).";

    public required GetMetricHistoryArguments Arguments { get; init; }

    public static GetMetricHistoryToolRequest Create(GetMetricHistoryArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetMetricHistoryResponse
{
    public required IReadOnlyList<ChatMetricBucket> Buckets { get; init; }
}

public sealed record ChatMetricBucket
{
    public required DateTime Start { get; init; }

    public required double Avg { get; init; }

    public required double Min { get; init; }

    public required double Max { get; init; }

    public required long Count { get; init; }
}

public sealed class GetMetricHistoryArgumentsValidator : AbstractValidator<GetMetricHistoryArguments>
{
    public GetMetricHistoryArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);
        this.AddIdRule(arguments => arguments.MetricDefinitionId);
        this.AddRangeRule(arguments => arguments.From, arguments => arguments.To);
    }
}

public sealed class GetMetricHistoryToolRequestValidator : AbstractValidator<GetMetricHistoryToolRequest>
{
    public GetMetricHistoryToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetMetricHistoryArgumentsValidator());
    }
}

public sealed class GetMetricHistoryToolPreHandlerCheck : PassThroughPreHandlerCheck<GetMetricHistoryToolRequest>;

/// <summary>
/// Reads the history of one metric in <see cref="AgentOptions.PlannerHistoryBucketCount"/> buckets. A range above
/// <see cref="AgentOptions.HistoryMaxRangeHours"/> hours is rejected to the model.
/// </summary>
/// <param name="reads">Reads the history with the token of the user.</param>
/// <param name="options">Holds the bucket count and the range limit.</param>
public sealed class GetMetricHistoryToolRequestHandler(IChatReads reads, IOptions<AgentOptions> options)
    : IRequestHandler<GetMetricHistoryToolRequest, GetMetricHistoryResponse>
{
    private readonly IChatReads _reads = reads;
    private readonly AgentOptions _options = options.Value;

    public Task<GetMetricHistoryResponse> HandleAsync(
        GetMetricHistoryToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (request.Arguments.To - request.Arguments.From > TimeSpan.FromHours(_options.HistoryMaxRangeHours))
        {
            throw new UnprocessableException(AgentMessages.HistoryRangeTooLong(_options.HistoryMaxRangeHours));
        }

        return _reads.GetMetricHistoryAsync(
            request.Arguments,
            _options.PlannerHistoryBucketCount,
            cancellationToken);
    }
}
