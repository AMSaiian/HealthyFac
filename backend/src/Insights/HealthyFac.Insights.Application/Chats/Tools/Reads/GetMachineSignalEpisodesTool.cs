using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetMachineSignalEpisodesArguments
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
public sealed record GetMachineSignalEpisodesToolRequest
    : IAgentToolRequest<GetMachineSignalEpisodesToolRequest, GetMachineSignalEpisodesArguments,
        GetMachineSignalEpisodesResponse>
{
    public const string ToolName = "get_machine_signal_episodes";

    public const string ToolDescription =
        "Reads the signal episodes of a machine that overlap a range: the rule, the severity, the state, the "
        + "metric or status that caused it, and when it opened and resolved. "
        + AgentToolTexts.UtcTimes;

    public required GetMachineSignalEpisodesArguments Arguments { get; init; }

    public static GetMachineSignalEpisodesToolRequest Create(
        GetMachineSignalEpisodesArguments arguments,
        ToolContext context
    ) =>
        new() { Arguments = arguments };
}

public sealed record GetMachineSignalEpisodesResponse
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

public sealed class GetMachineSignalEpisodesArgumentsValidator : AbstractValidator<GetMachineSignalEpisodesArguments>
{
    public GetMachineSignalEpisodesArgumentsValidator()
    {
        this.AddIdRule(arguments => arguments.MachineId);
        this.AddRangeRule(arguments => arguments.From, arguments => arguments.To);
    }
}

public sealed class
    GetMachineSignalEpisodesToolRequestValidator : AbstractValidator<GetMachineSignalEpisodesToolRequest>
{
    public GetMachineSignalEpisodesToolRequestValidator()
    {
        RuleFor(request => request.Arguments).SetValidator(new GetMachineSignalEpisodesArgumentsValidator());
    }
}

public sealed class
    GetMachineSignalEpisodesToolPreHandlerCheck : PassThroughPreHandlerCheck<GetMachineSignalEpisodesToolRequest>;

public sealed class GetMachineSignalEpisodesToolRequestHandler(IChatReads reads)
    : IRequestHandler<GetMachineSignalEpisodesToolRequest, GetMachineSignalEpisodesResponse>
{
    private readonly IChatReads _reads = reads;

    public Task<GetMachineSignalEpisodesResponse> HandleAsync(
        GetMachineSignalEpisodesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    ) =>
        _reads.GetSignalEpisodesAsync(request.Arguments, cancellationToken);
}
