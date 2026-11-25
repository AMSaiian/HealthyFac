namespace HealthyFac.Insights.Application.Chats.Turns;

/// <summary>
/// The main model of a chat turn. It streams the answer in pieces.
/// </summary>
public interface IChatModel
{
    /// <summary>The name of the model. The run stores it.</summary>
    string ModelName { get; }

    /// <summary>
    /// Streams the answer to the last message of the history.
    /// </summary>
    /// <param name="systemPrompt">The system prompt.</param>
    /// <param name="history">The messages of the chat in order. The last one is the new user message.</param>
    /// <param name="cancellationToken">The token that stops the stream.</param>
    /// <returns>The pieces of the answer.</returns>
    /// <exception cref="Ingest.Processing.Failures.ProviderUnavailableException">
    /// Thrown when the provider fails or rejects the call.
    /// </exception>
    IAsyncEnumerable<ChatModelChunk> StreamAsync(
        string systemPrompt,
        IReadOnlyList<ChatPromptMessage> history,
        CancellationToken cancellationToken
    );
}
