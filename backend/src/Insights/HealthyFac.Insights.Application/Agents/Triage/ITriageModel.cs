namespace HealthyFac.Insights.Application.Agents.Triage;

/// <summary>
/// Asks the small triage model for the verdict on a case.
/// </summary>
public interface ITriageModel
{
    /// <summary>The name of the model. The run stores it.</summary>
    string ModelName { get; }

    /// <summary>
    /// Sends the system prompt and the data block and reads the structured answer. The call is safe to repeat.
    /// </summary>
    /// <param name="systemPrompt">The system prompt of the triage.</param>
    /// <param name="userMessage">The case as a data block.</param>
    /// <param name="cancellationToken">The token that cancels the call.</param>
    /// <returns>The verdict, the reason, and the tokens.</returns>
    Task<TriageModelAnswer> AskAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken);
}
