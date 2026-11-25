using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetSignalEpisodesArguments
{
    public required Guid MachineId { get; init; }

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
public sealed record GetSignalEpisodesToolRequest
    : IAgentToolRequest<GetSignalEpisodesToolRequest, GetSignalEpisodesArguments, GetSignalEpisodesResponse>
{
    public const string ToolName = "get_machine_signal_episodes";

    public const string ToolDescription =
        "Reads the signal episodes of a machine that overlap a range: the rule, the severity, the state, the "
        + "metric or status that caused it, and when it opened and resolved.";

    public required GetSignalEpisodesArguments Arguments { get; init; }

    public static GetSignalEpisodesToolRequest Create(GetSignalEpisodesArguments arguments, ToolContext context) =>
        new() { Arguments = arguments };
}

public sealed record GetSignalEpisodesResponse
{
    public required IReadOnlyList<ChatSignalEpisode> Episodes { get; init; }
}

public sealed record ChatSignalEpisode
{
    public required Guid EpisodeId { get; init; }

    public required string Rule { get; init; }

    public required string Severity { get; init; }

    public required string State { get; init; }

    public string? MetricKey { get; init; }

    public string? StatusCode { get; init; }

    public string? PhaseCode { get; init; }

    public double? Value { get; init; }

    public double? Limit { get; init; }

    public double? Z { get; init; }

    public double? ForecastSeconds { get; init; }

    public required DateTime OpenedAt { get; init; }

    public DateTime? ResolvedAt { get; init; }
}

public sealed class GetSignalEpisodesArgumentsValidator : AbstractValidator<GetSignalEpisodesArguments>
{
    public GetSignalEpisodesArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);
        this.AddRangeRule(arguments => arguments.From, arguments => arguments.To);
    }
}

public sealed class GetSignalEpisodesToolRequestValidator : AbstractValidator<GetSignalEpisodesToolRequest>
{
    public GetSignalEpisodesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetSignalEpisodesArgumentsValidator());
    }
}

public sealed class GetSignalEpisodesToolPreHandlerCheck : PassThroughPreHandlerCheck<GetSignalEpisodesToolRequest>;

public sealed class GetSignalEpisodesToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetSignalEpisodesToolRequest, GetSignalEpisodesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetSignalEpisodesResponse> HandleAsync(
        GetSignalEpisodesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetSignalEpisodesAsync(request.Arguments, cancellationToken);
}
