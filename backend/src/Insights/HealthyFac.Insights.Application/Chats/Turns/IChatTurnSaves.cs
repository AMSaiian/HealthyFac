using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.ToolCalls;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Stores the final state of a chat turn: the chat, its messages, and the run row, in one save.
/// </summary>
public interface IChatTurnSaves
{
    /// <summary>
    /// Adds the run row and the tool call rows and saves every tracked change.
    /// </summary>
    /// <param name="run">The run of the turn.</param>
    /// <param name="toolCalls">The tool calls of the run, in the order of the calls.</param>
    /// <param name="cancellationToken">The token of the save.</param>
    Task SaveAsync(AgentRun run, IReadOnlyList<AgentToolCall> toolCalls, CancellationToken cancellationToken);
}
