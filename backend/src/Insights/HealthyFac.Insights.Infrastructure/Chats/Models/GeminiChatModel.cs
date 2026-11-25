using System.Runtime.CompilerServices;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Domain.Chats.Messages;
using HealthyFac.Insights.Infrastructure.Ai;
using HealthyFac.Insights.Infrastructure.Ingest.ProviderCalls;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Chats.Models;

/// <summary>
/// Streams the answer of the main chat model (the default chat client). A provider error before or during the stream
/// ends the stream with a <see cref="ProviderUnavailableException"/>. The stream is not repeated, because the user
/// already saw a part of the text. The message of the exception holds the status only.
/// </summary>
/// <param name="chatClient">The default chat client.</param>
/// <param name="options">Holds the name of the model and the answer token limit.</param>
public sealed class GeminiChatModel(IChatClient chatClient, IOptions<GeminiOptions> options) : IChatModel
{
    private const string UnavailableMessage = "The chat model stayed unavailable with the status {0}.";

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;

    public string ModelName => _options.ChatModel;

    public async IAsyncEnumerable<ChatModelChunk> StreamAsync(
        string systemPrompt,
        IReadOnlyList<ChatPromptMessage> history,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var messages = history
            .Select(message => new Microsoft.Extensions.AI.ChatMessage(
                message.Role == ChatMessageRole.User ? ChatRole.User : ChatRole.Assistant,
                message.Text))
            .ToList();
        var chatOptions = new ChatOptions
        {
            Instructions = systemPrompt,
            MaxOutputTokens = _options.MaxOutputTokens,
        };

        await using var enumerator = _chatClient.GetStreamingResponseAsync(messages, chatOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatModelChunk chunk;

            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }

                chunk = ToChunk(enumerator.Current);
            }
            catch (Exception exception) when (
                !cancellationToken.IsCancellationRequested
                && (ProviderFailures.IsTransient(exception) || ProviderFailures.IsPermanent(exception)))
            {
                throw new ProviderUnavailableException(
                    string.Format(UnavailableMessage, ProviderFailures.DescribeStatus(exception)),
                    exception);
            }

            yield return chunk;
        }
    }

    private static ChatModelChunk ToChunk(ChatResponseUpdate update)
    {
        var usage = update.Contents.OfType<UsageContent>().LastOrDefault()?.Details;

        return new ChatModelChunk
        {
            Text = update.Text ?? string.Empty,
            IsLengthLimited = update.FinishReason == ChatFinishReason.Length,
            InputTokens = (int)(usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(usage?.OutputTokenCount ?? 0),
        };
    }
}
