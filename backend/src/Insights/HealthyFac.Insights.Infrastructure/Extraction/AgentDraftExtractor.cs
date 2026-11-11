using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Infrastructure.Ai;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Extraction;

/// <summary>
/// Asks the model for the structured extraction through a Microsoft Agent Framework agent over the registered chat
/// client. The agent has no tools. The response schema comes from <see cref="ExtractionResult"/>, with
/// <c>additionalProperties: false</c> on every object, and the temperature is 0.
/// </summary>
/// <remarks>
/// When the answer does not parse or does not match the schema, the agent gets one repair turn in the same thread with
/// the error text. A second failure throws <see cref="InvalidExtractionException"/>. The error text holds the path of
/// the schema failure and never the answer of the model.
/// </remarks>
/// <param name="chatClient">The chat client of the model.</param>
/// <param name="options">Holds the answer token limit.</param>
public sealed class AgentDraftExtractor(IChatClient chatClient, IOptions<GeminiOptions> options) : IDraftExtractor
{
    public const string AgentName = "draft-extractor";

    private const int MaxErrorLength = 500;
    private const string RepairMessageFormat =
        "Your last answer did not match the response schema: {0}. Return the corrected JSON only.";

    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _options = options.Value;

    public async Task<ExtractionResult> ExtractAsync(ExtractionInput input, CancellationToken cancellationToken)
    {
        var agent = CreateAgent(ExtractionPrompt.BuildSystemPrompt(input));
        var session = await agent.CreateSessionAsync(cancellationToken);

        var first = await TryRunAsync(agent, ExtractionPrompt.BuildUserMessage(input), session, cancellationToken);
        if (first.Result is not null)
        {
            return first.Result;
        }

        var second = await TryRunAsync(
            agent,
            string.Format(RepairMessageFormat, first.Error),
            session,
            cancellationToken);

        return second.Result ?? throw new InvalidExtractionException(second.Error!);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

        return options;
    }

    private static async Task<Attempt> TryRunAsync(
        AIAgent agent,
        string message,
        AgentSession session,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var response = await agent.RunAsync<ExtractionResult>(
                message,
                session,
                JsonOptions,
                cancellationToken: cancellationToken);

            return new Attempt { Result = response.Result };
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            var text = exception.Message.Length > MaxErrorLength
                ? exception.Message[..MaxErrorLength]
                : exception.Message;

            return new Attempt { Error = text };
        }
    }

    private AIAgent CreateAgent(string systemPrompt)
    {
        var agent = new ChatClientAgent(
            _chatClient,
            new ChatClientAgentOptions
            {
                Name = AgentName,
                ChatOptions = new ChatOptions
                {
                    Instructions = systemPrompt,
                    Temperature = 0,
                    MaxOutputTokens = _options.MaxOutputTokens,
                    Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low },
                    ResponseFormat = ChatResponseFormat.ForJsonSchema<ExtractionResult>(JsonOptions),
                    Tools = [],
                },
            });

        return agent.AsBuilder()
            .UseOpenTelemetry(
                sourceName: AiServiceExtensions.SourceName,
                configure: telemetry => telemetry.EnableSensitiveData = false)
            .Build();
    }

    private sealed record Attempt
    {
        public ExtractionResult? Result { get; init; }

        public string? Error { get; init; }
    }
}
