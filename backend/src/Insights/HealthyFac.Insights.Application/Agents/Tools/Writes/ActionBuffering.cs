using System.Text.Json;
using HealthyFac.Insights.Application.Agents.Runs;
using HealthyFac.Insights.Domain.Agents.Actions;
using HealthyFac.Insights.Domain.Cases.Signals;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Insights.Application.Agents.Tools.Writes;

/// <summary>
/// The buffer of the write actions of a run. A write tool never changes a case: it stores an <c>AgentAction</c> with
/// the state <c>buffered</c>, and <see cref="RunCommit"/> applies the actions when the run completes. The unique key
/// of the run and the call ID makes a repeated call add no second action.
/// </summary>
public static class ActionBuffering
{
    public const string UnknownEpisodeMessage = "An episode ID is not a signal of this case.";
    public const string EpisodeNotOpenMessage = "An episode is resolved, so no work order covers it.";
    public const string EpisodeCoveredMessage = "An episode is already covered by a work order draft or a work order.";
    public const string EpisodeBufferedMessage = "An episode is already part of an earlier action of this run.";

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Serializes a payload as the JSON text of an action.
    /// </summary>
    /// <param name="payload">The payload record.</param>
    /// <typeparam name="TPayload">The type of the payload.</typeparam>
    /// <returns>The JSON text.</returns>
    public static string Serialize<TPayload>(TPayload payload) => JsonSerializer.Serialize(payload, Json);

    /// <summary>
    /// Reads the payload of an action.
    /// </summary>
    /// <param name="action">The action.</param>
    /// <typeparam name="TPayload">The type of the payload.</typeparam>
    /// <returns>The payload.</returns>
    public static TPayload Deserialize<TPayload>(AgentAction action) =>
        JsonSerializer.Deserialize<TPayload>(action.Payload, Json)!;

    /// <summary>
    /// Finds the action that the same call buffered before.
    /// </summary>
    /// <param name="dbContext">Reads the actions.</param>
    /// <param name="runId">The run of the call.</param>
    /// <param name="callId">The ID that the model gave the call.</param>
    /// <param name="cancellationToken">The token that cancels the query.</param>
    /// <returns>The ID of the action, or <see langword="null"/> when the call buffered nothing yet.</returns>
    public static async Task<Guid?> FindAsync(
        IInsightsDbContext dbContext,
        Guid runId,
        string callId,
        CancellationToken cancellationToken
    )
    {
        var existing = await dbContext.AgentActions
            .AsNoTracking()
            .Where(action => action.RunId == runId && action.CallId == callId)
            .Select(action => new { action.Id })
            .FirstOrDefaultAsync(cancellationToken);

        return existing?.Id;
    }

    /// <summary>
    /// Stores a new buffered action.
    /// </summary>
    /// <param name="dbContext">Saves the action.</param>
    /// <param name="runId">The run of the call.</param>
    /// <param name="callId">The ID that the model gave the call.</param>
    /// <param name="kind">The kind of the write.</param>
    /// <param name="payload">The JSON payload.</param>
    /// <param name="cancellationToken">The token that cancels the save.</param>
    /// <returns>The ID of the action.</returns>
    public static async Task<Guid> AddAsync(
        IInsightsDbContext dbContext,
        Guid runId,
        string callId,
        AgentActionKind kind,
        string payload,
        CancellationToken cancellationToken
    )
    {
        var action = new AgentAction(runId, callId, kind, payload);
        dbContext.AgentActions.Add(action);
        await dbContext.SaveChangesAsync(cancellationToken);

        return action.Id;
    }

    /// <summary>
    /// Checks that every episode is a signal of the case that a work order can still cover: it is open, it is neither
    /// <c>drafted</c> nor <c>linked</c>, and no earlier action of the same run holds it.
    /// </summary>
    /// <param name="dbContext">Reads the signals and the actions.</param>
    /// <param name="caseId">The case of the run.</param>
    /// <param name="runId">The run of the call.</param>
    /// <param name="episodeIds">The episode IDs that the model gave.</param>
    /// <param name="cancellationToken">The token that cancels the queries.</param>
    /// <returns>A task that completes when every episode passes.</returns>
    /// <exception cref="UnprocessableException">Thrown for the first episode that fails a rule.</exception>
    public static async Task EnsureEpisodesAsync(
        IInsightsDbContext dbContext,
        Guid caseId,
        Guid runId,
        IReadOnlyList<Guid> episodeIds,
        CancellationToken cancellationToken
    )
    {
        var signals = await dbContext.MachineSignals
            .AsNoTracking()
            .Where(signal => signal.CaseId == caseId && episodeIds.Contains(signal.Id))
            .ToListAsync(cancellationToken);

        if (signals.Count != episodeIds.Distinct().Count())
        {
            throw new UnprocessableException(UnknownEpisodeMessage);
        }

        if (signals.Any(signal => !signal.IsOpen))
        {
            throw new UnprocessableException(EpisodeNotOpenMessage);
        }

        if (signals.Any(signal =>
            signal.Coverage == SignalCoverage.Drafted || signal.Coverage == SignalCoverage.Linked))
        {
            throw new UnprocessableException(EpisodeCoveredMessage);
        }

        var bufferedSignalIds = await ReadBufferedSignalIdsAsync(dbContext, runId, cancellationToken);

        if (episodeIds.Any(bufferedSignalIds.Contains))
        {
            throw new UnprocessableException(EpisodeBufferedMessage);
        }
    }

    private static async Task<HashSet<Guid>> ReadBufferedSignalIdsAsync(
        IInsightsDbContext dbContext,
        Guid runId,
        CancellationToken cancellationToken
    )
    {
        var actions = await dbContext.AgentActions
            .AsNoTracking()
            .Where(action => action.RunId == runId
                && action.State == AgentActionState.Buffered
                && (action.Kind == AgentActionKind.ProposeWorkOrder || action.Kind == AgentActionKind.LinkWorkOrder))
            .ToListAsync(cancellationToken);

        return
        [
            .. actions
                .SelectMany(action => action.Kind == AgentActionKind.ProposeWorkOrder
                    ? Deserialize<ProposeWorkOrderPayload>(action).SignalIds
                    : Deserialize<LinkWorkOrderPayload>(action).SignalIds),
        ];
    }
}
