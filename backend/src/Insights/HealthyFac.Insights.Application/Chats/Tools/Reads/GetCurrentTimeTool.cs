using HealthyFac.Insights.Application.Agents.Tools;
using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Insights.Application.Chats.Tools.Reads;

public sealed record GetCurrentTimeArguments;

[AgentTool(role: "chat", ToolName, Description = ToolDescription, IsRead = true, Activity = "general")]
public sealed record GetCurrentTimeToolRequest
    : IAgentToolRequest<GetCurrentTimeToolRequest, GetCurrentTimeArguments, GetCurrentTimeResponse>
{
    public const string ToolName = "get_current_time";

    public const string ToolDescription =
        "Reads the current time: UTC, the company time zone, and the local time there. Call it before you compute a "
        + "relative time, such as the last 2 hours or today.";

    public static GetCurrentTimeToolRequest Create(GetCurrentTimeArguments arguments, ToolContext context) => new();
}

public sealed record GetCurrentTimeResponse
{
    public required DateTime UtcNow { get; init; }

    /// <summary>The IANA ID of the company time zone, or <see langword="null"/> when it is not known.</summary>
    public string? TimeZoneId { get; init; }

    /// <summary>The current time in the company time zone, or <see langword="null"/> when it is not known.</summary>
    public DateTimeOffset? LocalTime { get; init; }
}

public sealed class GetCurrentTimeToolPreHandlerCheck : PassThroughPreHandlerCheck<GetCurrentTimeToolRequest>;

/// <summary>
/// Reads the current time in UTC and in the time zone of the company. A failed zone read never fails the tool: the
/// answer then holds the UTC time only.
/// </summary>
/// <param name="timeZoneReader">Reads the time zone of the company.</param>
/// <param name="timeProvider">Provides the current time.</param>
/// <param name="logger">Logs a zone that could not be read.</param>
public sealed class GetCurrentTimeToolRequestHandler(
    ICompanyTimeZoneReader timeZoneReader,
    TimeProvider timeProvider,
    ILogger<GetCurrentTimeToolRequestHandler> logger
) : IRequestHandler<GetCurrentTimeToolRequest, GetCurrentTimeResponse>
{
    private readonly ICompanyTimeZoneReader _timeZoneReader = timeZoneReader;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<GetCurrentTimeToolRequestHandler> _logger = logger;

    public async Task<GetCurrentTimeResponse> HandleAsync(
        GetCurrentTimeToolRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var now = _timeProvider.GetUtcNow();

        try
        {
            var zoneId = await _timeZoneReader.ReadTimeZoneIdAsync(cancellationToken);

            if (TimeZoneInfo.TryFindSystemTimeZoneById(zoneId, out var zone))
            {
                return new GetCurrentTimeResponse
                {
                    UtcNow = now.UtcDateTime,
                    TimeZoneId = zone.Id,
                    LocalTime = TimeZoneInfo.ConvertTime(now, zone),
                };
            }

            _logger.LogWarning(message: "The host does not know the time zone of the company.");
        }
        catch (ProviderUnavailableException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(message: "The time zone of the company could not be read.");
        }

        return new GetCurrentTimeResponse { UtcNow = now.UtcDateTime };
    }
}
