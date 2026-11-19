using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Insights.Domain.Agents.ToolCalls;

/// <summary>
/// One tool call of an agent run, kept in the AI log. The sequence orders the calls of a run, and the call ID is the
/// ID that the model gave the call. The arguments and the result are JSON text.
/// </summary>
public class AgentToolCall : IEntity
{
    /// <summary>
    /// Creates the log row of a tool call.
    /// </summary>
    /// <param name="runId">The run of the call.</param>
    /// <param name="sequence">The position of the call in the run, from 1.</param>
    /// <param name="callId">The ID that the model gave the call.</param>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="arguments">The arguments as JSON text.</param>
    /// <param name="outcome">How the call ended.</param>
    /// <param name="result">The result as JSON text.</param>
    /// <param name="durationMilliseconds">The time of the call in milliseconds.</param>
    public AgentToolCall(
        Guid runId,
        int sequence,
        string callId,
        string toolName,
        string arguments,
        ToolCallOutcome outcome,
        string result,
        int durationMilliseconds
    )
    {
        RunId = runId;
        Sequence = sequence;
        CallId = callId;
        ToolName = toolName;
        Arguments = arguments;
        Outcome = outcome;
        Result = result;
        DurationMilliseconds = durationMilliseconds;
    }

    public Guid Id { get; private set; } = Guid.CreateVersion7();

    public Guid RunId { get; private set; }

    public int Sequence { get; private set; }

    public string CallId { get; private set; }

    public string ToolName { get; private set; }

    public string Arguments { get; private set; }

    public ToolCallOutcome Outcome { get; private set; }

    public string Result { get; private set; }

    public int DurationMilliseconds { get; private set; }
}
