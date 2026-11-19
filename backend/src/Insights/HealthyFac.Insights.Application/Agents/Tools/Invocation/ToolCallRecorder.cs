using HealthyFac.Insights.Domain.Agents.ToolCalls;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// Stores each tool call of a run in the AI log at once, so a failed run keeps the calls that it made.
/// </summary>
/// <param name="runId">The run of the calls.</param>
/// <param name="store">
/// Stores one row. It builds the row with the next sequence of the run. <see cref="ForDatabase"/> reads the sequence
/// from the stored calls of the run, so a run that continues after a retry never repeats a sequence.
/// </param>
public sealed class ToolCallRecorder(Guid runId, Func<Func<int, AgentToolCall>, CancellationToken, Task> store)
{
    private readonly Guid _runId = runId;
    private readonly Func<Func<int, AgentToolCall>, CancellationToken, Task> _store = store;

    /// <summary>
    /// Creates a recorder that saves each row with the database context.
    /// </summary>
    /// <param name="dbContext">Saves the rows.</param>
    /// <param name="runId">The run of the calls.</param>
    /// <returns>The recorder.</returns>
    public static ToolCallRecorder ForDatabase(IInsightsDbContext dbContext, Guid runId) =>
        new(
            runId,
            async (createRow, cancellationToken) =>
            {
                var lastSequence = await dbContext.AgentToolCalls
                    .Where(call => call.RunId == runId)
                    .MaxAsync(call => (int?)call.Sequence, cancellationToken);
                dbContext.AgentToolCalls.Add(createRow((lastSequence ?? 0) + 1));
                await dbContext.SaveChangesAsync(cancellationToken);
            });

    /// <summary>
    /// Stores one call. The sequence counts from 1 in the order of the calls.
    /// </summary>
    /// <param name="callId">The ID that the model gave the call.</param>
    /// <param name="toolName">The name of the tool.</param>
    /// <param name="arguments">The arguments as JSON text.</param>
    /// <param name="outcome">How the call ended.</param>
    /// <param name="result">The result text.</param>
    /// <param name="duration">The time of the call.</param>
    /// <param name="cancellationToken">The token that cancels the save.</param>
    /// <returns>A task that completes when the row is saved.</returns>
    public async Task RecordAsync(
        string callId,
        string toolName,
        string arguments,
        ToolCallOutcome outcome,
        string result,
        TimeSpan duration,
        CancellationToken cancellationToken
    )
    {
        await _store(
            sequence => new AgentToolCall(
                _runId,
                sequence,
                callId,
                toolName,
                arguments,
                outcome,
                result,
                (int)duration.TotalMilliseconds),
            cancellationToken);
    }
}
