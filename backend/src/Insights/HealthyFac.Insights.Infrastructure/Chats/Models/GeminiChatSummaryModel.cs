using System.Text.Json;
using HealthyFac.Insights.Application.Agents;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Chats.Turns;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Chats.Models;

/// <summary>
/// Asks the small triage model (the keyed chat client of <see cref="AiClientKeys.Triage"/>) for the rolling summary of
/// a chat. The input holds the earlier summary and the messages in one data block and nothing else. The temperature is
/// 0, and the model has no tools. An empty answer ends the call with a <see cref="ProviderUnavailableException"/>.
/// </summary>
/// <param name="chatClient">The keyed triage chat client.</param>
/// <param name="options">Holds the answer token limit.</param>
/// <param name="prompts">Provides the system prompt.</param>
/// <param name="providerCalls">Retries a transient error.</param>
public sealed class GeminiChatSummaryModel(
    IChatClient chatClient,
    IOptions<GeminiOptions> options,
    IAgentPrompts prompts,
    IAgentProviderCalls providerCalls
) : IChatSummaryModel
{
    private const string ConversationTag = "conversation";
    private const string EmptyAnswerMessage = "The chat summary model answered with no text.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly IAgentPrompts _prompts = prompts;
    private readonly IAgentProviderCalls _providerCalls = providerCalls;

    public async Task<string> SummarizeAsync(
        string? previousSummary,
        IReadOnlyList<ChatPromptMessage> messages,
        CancellationToken cancellationToken
    )
    {
        var instructions = _prompts.ForChatSummary().Text;

        var input = AgentDataBlocks.Wrap(
            ConversationTag,
            JsonSerializer.Serialize(
                new
                {
                    previousSummary,
                    messages = messages.Select(message => new { role = message.Role.Value, text = message.Text }),
                },
                JsonOptions));

        var summary = await _providerCalls.ExecuteAsync(
            token => SendAsync(instructions, input, token),
            cancellationToken);

        return summary;
    }

    private async Task<string> SendAsync(string instructions, string input, CancellationToken cancellationToken)
    {
        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, input)],
            new ChatOptions
            {
                Instructions = instructions,
                Temperature = 0,
                MaxOutputTokens = _options.MaxOutputTokens,
                Tools = [],
            },
            cancellationToken);

        return string.IsNullOrWhiteSpace(response.Text)
            ? throw new ProviderUnavailableException(EmptyAnswerMessage, new InvalidOperationException())
            : response.Text;
    }
}
