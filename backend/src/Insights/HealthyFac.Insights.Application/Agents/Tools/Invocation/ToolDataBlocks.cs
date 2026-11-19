using HealthyFac.Insights.Application.Agents.Prompts;

namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// Wraps the text of a tool result in an escaped data block, so a text from a document or a work order cannot act as
/// an instruction.
/// </summary>
public static class ToolDataBlocks
{
    public const string Tag = "tool_result";

    /// <summary>
    /// Wraps the text.
    /// </summary>
    /// <param name="text">The result of the tool.</param>
    /// <returns>The block.</returns>
    public static string Wrap(string text) => AgentDataBlocks.Wrap(Tag, text);
}
