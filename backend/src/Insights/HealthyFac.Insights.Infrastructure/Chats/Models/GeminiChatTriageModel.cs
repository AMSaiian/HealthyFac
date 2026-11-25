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
/// Asks the small triage model (the keyed chat client of <see cref="AiClientKeys.Triage"/>) for the verdict on a chat
/// message. The input holds the text and the link labels in one data block and nothing else. The response schema comes
/// from <see cref="ChatTriageVerdict"/>, the temperature is 0, and the model has no tools. An answer outside the schema
/// ends the call with a <see cref="ProviderUnavailableException"/>, because the turn must fail closed.
/// </summary>
/// <param name="chatClient">The keyed triage chat client.</param>
/// <param name="options">Holds the name of the model and the answer token limit.</param>
/// <param name="prompts">Provides the system prompt.</param>
/// <param name="providerCalls">Retries a transient error.</param>
public sealed class GeminiChatTriageModel(
    IChatClient chatClient,
    IOptions<GeminiOptions> options,
    IAgentPrompts prompts,
    IAgentProviderCalls providerCalls
) : IChatTriageModel
{
    private const string MessageTag = "message";
    private const string InvalidAnswerMessage = "The chat triage model answer did not match the response schema.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;
    private readonly IAgentPrompts _prompts = prompts;
    private readonly IAgentProviderCalls _providerCalls = providerCalls;

    public string ModelName => _options.TriageModel;

    public async Task<ChatTriageAnswer> AskAsync(
        string text,
        IReadOnlyList<string> linkLabels,
        CancellationToken cancellationToken
    )
    {
        var instructions = _prompts.ForChatTriage().Text;

        var input = AgentDataBlocks.Wrap(
            MessageTag,
            JsonSerializer.Serialize(new { text, linkLabels }, JsonOptions));

        var answer = await _providerCalls.ExecuteAsync(
            token => SendAsync(instructions, input, token),
            cancellationToken);

        return answer;
    }

    private async Task<ChatTriageAnswer> SendAsync(
        string instructions,
        string input,
        CancellationToken cancellationToken
    )
    {
        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, input)],
            new ChatOptions
            {
                Instructions = instructions,
                Temperature = 0,
                MaxOutputTokens = _options.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<ChatTriageVerdict>(JsonOptions),
                Tools = [],
            },
            cancellationToken);

        ChatTriageVerdict? verdict;

        try
        {
            verdict = JsonSerializer.Deserialize<ChatTriageVerdict>(response.Text, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ProviderUnavailableException(InvalidAnswerMessage, exception);
        }

        return new ChatTriageAnswer
        {
            Verdict = verdict
                ?? throw new ProviderUnavailableException(
                    InvalidAnswerMessage,
                    new JsonException(InvalidAnswerMessage)),
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
        };
    }
}
