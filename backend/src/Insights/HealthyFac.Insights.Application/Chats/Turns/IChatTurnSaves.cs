using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Stores the final state of a chat turn: the chat, its messages, and the run row, in one save.
/// </summary>
public interface IChatTurnSaves
{
    /// <summary>
    /// Adds the run row and saves every tracked change.
    /// </summary>
    /// <param name="run">The run of the turn.</param>
    /// <param name="cancellationToken">The token of the save.</param>
    Task SaveAsync(AgentRun run, CancellationToken cancellationToken);
}
