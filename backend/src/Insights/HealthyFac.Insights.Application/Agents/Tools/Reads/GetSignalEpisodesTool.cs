using FluentValidation;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Signals;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Application.Agents.Tools.Reads;

/// <summary>The arguments of <c>get_signal_episodes</c>. The tool takes none: the options set the range.</summary>
public sealed record GetSignalEpisodesArguments;

[AgentTool(role: "planner", ToolName, Description = ToolDescription, IsRead = true)]
[DownstreamScopes("telemetry")]
public sealed record GetSignalEpisodesToolRequest
    : IAgentToolRequest<GetSignalEpisodesToolRequest, GetSignalEpisodesArguments, GetSignalEpisodesToolResponse>
{
    public const string ToolName = "get_signal_episodes";

    public const string ToolDescription =
        "Lists the most recent signal episodes of the machine (thresholds, drifts, trends, statuses, silence) with "
        + "their values and times.";

    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public static GetSignalEpisodesToolRequest Create(GetSignalEpisodesArguments arguments, ToolContext context) =>
        new() { CompanyId = context.CompanyId, MachineId = context.RequireMachineId() };
}

public sealed record GetSignalEpisodesToolResponse
{
    public required IReadOnlyList<SignalEpisodeInfo> Episodes { get; init; }
}

public class GetSignalEpisodesToolRequestValidator : AbstractValidator<GetSignalEpisodesToolRequest>
{
    public GetSignalEpisodesToolRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(AgentMessages.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(AgentMessages.MachineIdRequired);
    }
}

public sealed class GetSignalEpisodesToolPreHandlerCheck(
    IInsightsDbContext dbContext,
    ICurrentUserProvider currentUser
) : IPreHandlerCheck<GetSignalEpisodesToolRequest>
{
    private readonly IInsightsDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetSignalEpisodesToolRequest request, CancellationToken cancellationToken) =>
        ToolMachineScope.EnsureAsync(_dbContext, _currentUser, request.CompanyId, request.MachineId, cancellationToken);
}

/// <summary>
/// Reads the signal episodes of the last <see cref="AgentOptions.EpisodeRangeHours"/> hours from Telemetry and keeps
/// the newest <see cref="AgentOptions.EpisodeCount"/>.
/// </summary>
/// <param name="signals">Reads the episodes.</param>
/// <param name="options">Holds the range and the count.</param>
/// <param name="timeProvider">Provides now.</param>
public sealed class GetSignalEpisodesToolRequestHandler(
    ITelemetrySignals signals,
    IOptions<AgentOptions> options,
    TimeProvider timeProvider
) : IRequestHandler<GetSignalEpisodesToolRequest, GetSignalEpisodesToolResponse>
{
    private readonly ITelemetrySignals _signals = signals;
    private readonly AgentOptions _options = options.Value;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<GetSignalEpisodesToolResponse> HandleAsync(
        GetSignalEpisodesToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var to = _timeProvider.GetUtcNow().UtcDateTime;
        var episodes = await _signals.GetSignalEpisodesAsync(
            request.MachineId,
            to.AddHours(-_options.EpisodeRangeHours),
            to,
            cancellationToken);

        return new GetSignalEpisodesToolResponse
        {
            Episodes = episodes.OrderByDescending(episode => episode.OpenedAt).Take(_options.EpisodeCount).ToList(),
        };
    }
}
