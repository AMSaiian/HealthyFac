namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// Asks the small triage model for the verdict on a new chat message. The model sees only the new text and the
/// labels of its links, never the history and never tool data.
/// </summary>
public interface IChatTriageModel
{
    /// <summary>The name of the model.</summary>
    string ModelName { get; }

    /// <summary>
    /// Sends the text and the link labels and reads the structured answer. The call retries a transient provider
    /// error with the delays of <see cref="Agents.AgentOptions.ProviderRetryDelaysSeconds"/>.
    /// </summary>
    /// <param name="text">The new message text.</param>
    /// <param name="linkLabels">The labels of the links of the message.</param>
    /// <param name="cancellationToken">The token that cancels the call and the waits.</param>
    /// <returns>The verdict and the tokens.</returns>
    /// <exception cref="Ingest.Processing.Failures.ProviderUnavailableException">
    /// Thrown when the provider stays unavailable, rejects the call, or answers outside the schema.
    /// </exception>
    Task<ChatTriageAnswer> AskAsync(
        string text,
        IReadOnlyList<string> linkLabels,
        CancellationToken cancellationToken
    );
}
