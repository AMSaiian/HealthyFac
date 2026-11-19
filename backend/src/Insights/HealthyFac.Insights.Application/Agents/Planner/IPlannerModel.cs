using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Application.Agents.Planner;

/// <summary>
/// Runs the planner agent: the model reads the case, calls the tools in a loop, and ends with a text. The tools
/// buffer the writes, so the call itself returns only the tokens.
/// </summary>
public interface IPlannerModel
{
    /// <summary>The name of the model. The run stores it.</summary>
    string ModelName { get; }

    /// <summary>
    /// Runs the loop of the model and the tools. An implementation retries each model call on its own, never the
    /// whole loop, because a repeat of the loop would run the read tools again and keep buffered actions of the first
    /// pass.
    /// </summary>
    /// <param name="systemPrompt">The system prompt of the planner.</param>
    /// <param name="userMessage">The case as a data block.</param>
    /// <param name="tools">The tools of the planner role.</param>
    /// <param name="decisionTools">The write tools. The decision turn offers only these.</param>
    /// <param name="needsDecision">
    /// Tells, after the loop, whether the run still needs a decision (no write action is buffered). When it does,
    /// the implementation makes one more model call with <paramref name="decisionTools"/> and a required tool call.
    /// </param>
    /// <param name="cancellationToken">The token that cancels the run.</param>
    /// <returns>The tokens of the whole loop and the last text of the model.</returns>
    /// <exception cref="ProviderUnavailableException">
    /// Thrown when a model call stays unavailable after its retries.
    /// </exception>
    Task<PlannerModelResult> RunAsync(
        string systemPrompt,
        string userMessage,
        IReadOnlyList<AIFunction> tools,
        IReadOnlyList<AIFunction> decisionTools,
        Func<CancellationToken, Task<bool>> needsDecision,
        CancellationToken cancellationToken
    );
}

public sealed record PlannerModelResult
{
    public required int InputTokens { get; init; }

    public required int OutputTokens { get; init; }

    /// <summary>The last text of the model in the run, or <see langword="null"/> when it wrote none.</summary>
    public string? FinalText { get; init; }
}
