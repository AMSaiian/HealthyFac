using System.Text.Json;
using HealthyFac.Insights.Application.Agents.Triage;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Agents.Models;

/// <summary>
/// Asks the small triage model (the keyed chat client of <see cref="AiClientKeys.Triage"/>) for the verdict. The
/// response schema comes from <see cref="TriageVerdict"/>, the temperature is 0, and the model has no tools. An answer
/// that does not match the schema ends the call with a <see cref="ProviderUnavailableException"/>, because the run
/// must end at once. The message holds no text of the answer.
/// </summary>
/// <param name="chatClient">The keyed triage chat client.</param>
/// <param name="options">Holds the name of the model and the answer token limit.</param>
public sealed class GeminiTriageModel(IChatClient chatClient, IOptions<GeminiOptions> options) : ITriageModel
{
    private const string InvalidAnswerMessage = "The triage model answer did not match the response schema.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;

    public string ModelName => _options.TriageModel;

    public async Task<TriageModelAnswer> AskAsync(
        string systemPrompt,
        string userMessage,
        CancellationToken cancellationToken
    )
    {
        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, userMessage)],
            new ChatOptions
            {
                Instructions = systemPrompt,
                Temperature = 0,
                MaxOutputTokens = _options.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<TriageVerdict>(JsonOptions),
                Tools = [],
            },
            cancellationToken);

        TriageVerdict? verdict;

        try
        {
            verdict = JsonSerializer.Deserialize<TriageVerdict>(response.Text, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ProviderUnavailableException(InvalidAnswerMessage, exception);
        }

        return new TriageModelAnswer
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
