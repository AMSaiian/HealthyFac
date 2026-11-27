namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// The model that writes the rolling summary of a chat. It is the small triage model.
/// </summary>
public interface IChatSummaryModel
{
    /// <summary>
    /// Summarizes the older messages of a chat, together with the summary that already covers even older messages.
    /// </summary>
    /// <param name="previousSummary">The earlier summary, or <see langword="null"/> when the chat has none.</param>
    /// <param name="messages">The messages to add to the summary, in order.</param>
    /// <param name="cancellationToken">The token that stops the call.</param>
    /// <returns>The new summary text.</returns>
    /// <exception cref="Ingest.Processing.Failures.ProviderUnavailableException">
    /// Thrown when the provider fails or answers with no text.
    /// </exception>
    Task<string> SummarizeAsync(
        string? previousSummary,
        IReadOnlyList<ChatPromptMessage> messages,
        CancellationToken cancellationToken
    );
}
