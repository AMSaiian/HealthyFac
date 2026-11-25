using HealthyFac.Insights.Domain.Agents.Runs;

namespace HealthyFac.Insights.Application.Agents.Prompts;

/// <summary>
/// Provides the system prompt of each agent role.
/// </summary>
public interface IAgentPrompts
{
    /// <summary>
    /// Returns the prompt of a role.
    /// </summary>
    /// <param name="role">The role of the run.</param>
    /// <returns>The prompt with its version and hash.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the role has no prompt.</exception>
    AgentPrompt For(AgentRole role);

    /// <summary>
    /// Returns the prompt of the chat triage model, which is no agent role.
    /// </summary>
    /// <returns>The prompt with its version and hash.</returns>
    AgentPrompt ForChatTriage();
}
