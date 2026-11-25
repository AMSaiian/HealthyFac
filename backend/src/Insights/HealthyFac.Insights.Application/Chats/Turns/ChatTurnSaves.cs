using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Saves the turn through the Insights database context.
/// </summary>
/// <param name="dbContext">Holds the tracked chat and messages.</param>
public sealed class ChatTurnSaves(IInsightsDbContext dbContext) : IChatTurnSaves
{
    private readonly IInsightsDbContext _dbContext = dbContext;

    public async Task SaveAsync(
        AgentRun run,
        IReadOnlyList<AgentToolCall> toolCalls,
        CancellationToken cancellationToken
    )
    {
        _dbContext.AgentRuns.Add(run);
        _dbContext.AgentToolCalls.AddRange(toolCalls);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
