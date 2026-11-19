using System.Text;
using System.Text.Json;
using HealthyFac.Insights.Application.Agents.Prompts;
using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Ingest.Processing;
using HealthyFac.Insights.Application.Ingest.Screening;
using HealthyFac.Insights.Domain.Agents.Runs;
using HealthyFac.Insights.Infrastructure.Ai;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace HealthyFac.Insights.Infrastructure.Ingest.Screening;

/// <summary>
/// Screens the chunks with the small triage model (the keyed chat client of <see cref="AiClientKeys.Triage"/>). Each
/// call gets up to <see cref="ScreeningOptions.BatchSize"/> chunks, each in an escaped data block with its index, and
/// the model answers with one verdict per chunk (<c>index</c>, <c>isSafe</c>, <c>reason</c>). The calls go through
/// <see cref="IProviderCalls"/>. The first unsafe chunk stops the screening. The screener keeps what it did in
/// a <see cref="ScreeningRunLog"/>: the result carries it, and a <see cref="ProviderUnavailableException"/> carries it
/// for a failure. The ingest handler stores it as the <c>screener</c> run of the attempt (<c>completed</c> with the
/// verdict <c>safe</c> or <c>unsafe</c>, or <c>failed</c>) in the same save as the outcome of the attempt. An answer
/// that misses a chunk or names a chunk of another batch counts as a provider failure, because no retry of the file
/// can fix a model that does not follow its schema.
/// </summary>
/// <param name="chatClient">The triage chat client.</param>
/// <param name="geminiOptions">Holds the model name and the answer token limit.</param>
/// <param name="options">Holds the batch size.</param>
/// <param name="providers">Retries each model call.</param>
/// <param name="prompts">Provides the system prompt.</param>
/// <param name="timeProvider">Provides the start and the end time of the screening.</param>
public sealed class GeminiChunkScreener(
    IChatClient chatClient,
    IOptions<GeminiOptions> geminiOptions,
    IOptions<ScreeningOptions> options,
    IProviderCalls providers,
    IAgentPrompts prompts,
    TimeProvider timeProvider
) : IChunkScreener
{
    public const string ChunkTag = "chunk";

    private const string InvalidAnswerMessage = "The screener answer did not match the response schema.";
    private const string UnknownLocationFormat = "chunk {0}";
    private const int ReasonMaxLength = 1000;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IChatClient _chatClient = chatClient;
    private readonly GeminiOptions _geminiOptions = geminiOptions.Value;
    private readonly ScreeningOptions _options = options.Value;
    private readonly IProviderCalls _providers = providers;
    private readonly IAgentPrompts _prompts = prompts;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<ScreeningResult> ScreenAsync(
        IngestAttemptSnapshot attempt,
        IReadOnlyList<ConvertedChunk> chunks,
        CancellationToken cancellationToken
    )
    {
        var prompt = _prompts.For(AgentRole.Screener);
        var startedAt = _timeProvider.GetUtcNow().UtcDateTime;
        var inputTokens = 0;
        var outputTokens = 0;
        ChunkVerdict? firstUnsafe = null;

        try
        {
            for (var start = 0; start < chunks.Count && firstUnsafe is null; start += _options.BatchSize)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var count = Math.Min(_options.BatchSize, chunks.Count - start);
                var answer = await _providers.ExecuteAsync(
                    token => AskAsync(prompt.Text, chunks, start, count, token),
                    cancellationToken);
                inputTokens += answer.InputTokens;
                outputTokens += answer.OutputTokens;
                firstUnsafe = answer.Verdicts.Where(verdict => !verdict.IsSafe).MinBy(verdict => verdict.Index);
            }
        }
        catch (ProviderUnavailableException exception)
        {
            exception.ScreeningRun = CreateLog(
                prompt,
                startedAt,
                inputTokens,
                outputTokens,
                verdict: null,
                reason: null,
                AgentRunErrorCode.ProviderUnavailable);

            throw;
        }

        var reason = firstUnsafe?.Reason is { Length: > ReasonMaxLength } text
            ? text[..ReasonMaxLength]
            : firstUnsafe?.Reason;
        var log = CreateLog(
            prompt,
            startedAt,
            inputTokens,
            outputTokens,
            firstUnsafe is null ? AgentRunVerdict.Safe : AgentRunVerdict.Unsafe,
            reason,
            errorCode: null);

        return firstUnsafe is null
            ? new ScreeningResult { Run = log }
            : new ScreeningResult
            {
                UnsafeLocation = chunks[firstUnsafe.Index].Location
                    ?? string.Format(UnknownLocationFormat, firstUnsafe.Index + 1),
                UnsafeReason = firstUnsafe.Reason,
                Run = log,
            };
    }

    private static string BuildMessage(IReadOnlyList<ConvertedChunk> chunks, int start, int count)
    {
        var message = new StringBuilder();

        for (var index = start; index < start + count; index++)
        {
            message.AppendLine(AgentDataBlocks.Wrap(ChunkTag, $"{index}: {chunks[index].Text}"));
        }

        return message.ToString();
    }

    private ScreeningRunLog CreateLog(
        AgentPrompt prompt,
        DateTime startedAt,
        int inputTokens,
        int outputTokens,
        AgentRunVerdict? verdict,
        string? reason,
        AgentRunErrorCode? errorCode
    ) =>
        new()
        {
            Model = _geminiOptions.TriageModel,
            PromptVersion = prompt.Version,
            PromptHash = prompt.Hash,
            StartedAt = startedAt,
            FinishedAt = _timeProvider.GetUtcNow().UtcDateTime,
            Verdict = verdict,
            VerdictReason = reason,
            ErrorCode = errorCode,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
        };

    private async Task<BatchAnswer> AskAsync(
        string systemPrompt,
        IReadOnlyList<ConvertedChunk> chunks,
        int start,
        int count,
        CancellationToken cancellationToken
    )
    {
        var response = await _chatClient.GetResponseAsync(
            [new ChatMessage(ChatRole.User, BuildMessage(chunks, start, count))],
            new ChatOptions
            {
                Instructions = systemPrompt,
                Temperature = 0,
                MaxOutputTokens = _geminiOptions.MaxOutputTokens,
                ResponseFormat = ChatResponseFormat.ForJsonSchema<ScreeningAnswer>(JsonOptions),
                Tools = [],
            },
            cancellationToken);

        var verdicts = Parse(response.Text, start, count);

        return new BatchAnswer
        {
            Verdicts = verdicts,
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0),
        };
    }

    private static List<ChunkVerdict> Parse(string text, int start, int count)
    {
        ScreeningAnswer? answer;

        try
        {
            answer = JsonSerializer.Deserialize<ScreeningAnswer>(text, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new ProviderUnavailableException(InvalidAnswerMessage, exception);
        }

        var verdicts = answer?.Verdicts;
        var expected = Enumerable.Range(start, count).ToList();

        if (verdicts is null
            || !expected.All(index => verdicts.Any(verdict => verdict.Index == index))
            || verdicts.Any(verdict => !expected.Contains(verdict.Index)))
        {
            throw new ProviderUnavailableException(InvalidAnswerMessage, new JsonException(InvalidAnswerMessage));
        }

        return verdicts;
    }

    /// <summary>The structured answer of the screener: one verdict for each chunk of the batch.</summary>
    public sealed record ScreeningAnswer
    {
        public required List<ChunkVerdict> Verdicts { get; init; }
    }

    /// <summary>The verdict for one chunk. The index is the position of the chunk in the file.</summary>
    public sealed record ChunkVerdict
    {
        public required int Index { get; init; }

        public required bool IsSafe { get; init; }

        public required string Reason { get; init; }
    }

    private sealed record BatchAnswer
    {
        public required List<ChunkVerdict> Verdicts { get; init; }

        public required int InputTokens { get; init; }

        public required int OutputTokens { get; init; }
    }
}
