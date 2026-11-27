using HealthyFac.Insights.Application.Agents.Tools.Invocation;
using HealthyFac.Insights.Domain.Agents.ToolCalls;

namespace HealthyFac.Insights.Application.Chats.Tools;

/// <summary>
/// Keeps the tool call rows of a chat turn in memory. The run row is first stored in the final save of the turn, so a
/// row that the recorder saved at once would break the foreign key of the run. The final save adds the rows with the
/// run, and nothing is saved in the middle of the turn.
/// </summary>
public sealed class ChatToolCallBuffer
{
    private readonly List<AgentToolCall> _rows = [];

    /// <summary>
    /// Creates the buffer and its recorder for the run.
    /// </summary>
    /// <param name="runId">The run of the calls.</param>
    public ChatToolCallBuffer(Guid runId)
    {
        Recorder = new ToolCallRecorder(
            runId,
            (createRow, _) =>
            {
                _rows.Add(createRow(_rows.Count + 1));

                return Task.CompletedTask;
            });
    }

    public ToolCallRecorder Recorder { get; }

    /// <summary>The rows of the calls in the order of the calls.</summary>
    public IReadOnlyList<AgentToolCall> Rows => _rows;
}
