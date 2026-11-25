using System.Runtime.CompilerServices;
using HealthyFac.Insights.Application.Chats.Tools;
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
        ChatTurnTools tools,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        var messages = history
            .Select(message => new Microsoft.Extensions.AI.ChatMessage(
                message.Role == ChatMessageRole.User ? ChatRole.User : ChatRole.Assistant,
                message.Text))
            .ToList();
        var totals = new UsageTotals();
        var tracked = new UsageTrackingChatClient(_chatClient, totals);
        var invoking = new FunctionInvokingChatClient(tracked);
        var updates = new List<ChatResponseUpdate>();

        await foreach (var update in StreamUpdatesAsync(
            invoking, messages, CreateOptions(systemPrompt, tools.Functions), cancellationToken))
        {
            updates.Add(update);

            yield return ToChunk(update);
        }

        if (tools.IsLimitReached && !tools.IsNoAnswerReported)
        {
            messages.AddRange(updates.ToChatResponse().Messages);
            CompleteToolResults(messages);

            await foreach (var update in StreamUpdatesAsync(
                tracked, messages, CreateOptions(systemPrompt, []), cancellationToken))
            {
                yield return ToChunk(update);
            }
        }

        yield return new ChatModelChunk { InputTokens = (int)totals.Input, OutputTokens = (int)totals.Output };
    }

    /// <summary>
    /// Gives each tool call that has no result the text of the call limit. The loop ends after the call at the limit,
    /// so the later calls of the same batch got no result, and the provider rejects a result turn with fewer parts than
    /// its call turn.
    /// </summary>
    private static void CompleteToolResults(List<Microsoft.Extensions.AI.ChatMessage> messages)
    {
        var answered = messages.SelectMany(message => message.Contents)
            .OfType<FunctionResultContent>()
            .Select(result => result.CallId)
            .ToHashSet();

        for (var index = 0; index < messages.Count; index++)
        {
            var missing = messages[index]
                .Contents.OfType<FunctionCallContent>()
                .Where(call => !answered.Contains(call.CallId))
                .Select(call => new FunctionResultContent(call.CallId, ChatTurnTools.LimitReachedText))
                .ToList();

            if (missing.Count == 0)
            {
                continue;
            }

            var hasResultMessage = index + 1 < messages.Count && messages[index + 1].Role == ChatRole.Tool;
            if (hasResultMessage)
            {
                foreach (var result in missing)
                {
                    messages[index + 1].Contents.Add(result);
                }
            }
            else
            {
                messages.Insert(index + 1, new Microsoft.Extensions.AI.ChatMessage(ChatRole.Tool, [.. missing]));
            }
        }
    }

    private ChatOptions CreateOptions(string systemPrompt, IReadOnlyList<AIFunction> functions) =>
        new()
        {
            Instructions = systemPrompt,
            MaxOutputTokens = _options.MaxOutputTokens,
            Tools = functions.Count == 0 ? null : [.. functions],
        };

    private static async IAsyncEnumerable<ChatResponseUpdate> StreamUpdatesAsync(
        IChatClient client,
        List<Microsoft.Extensions.AI.ChatMessage> messages,
        ChatOptions chatOptions,
        [EnumeratorCancellation] CancellationToken cancellationToken
    )
    {
        await using var enumerator = client.GetStreamingResponseAsync(messages, chatOptions, cancellationToken)
            .GetAsyncEnumerator(cancellationToken);

        while (true)
        {
            ChatResponseUpdate update;

            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    yield break;
                }

                update = enumerator.Current;
            }
            catch (Exception exception) when (
                !cancellationToken.IsCancellationRequested
                && (ProviderFailures.IsTransient(exception) || ProviderFailures.IsPermanent(exception)))
            {
                throw new ProviderUnavailableException(
                    string.Format(UnavailableMessage, ProviderFailures.DescribeStatus(exception)),
                    exception);
            }

            yield return update;
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
