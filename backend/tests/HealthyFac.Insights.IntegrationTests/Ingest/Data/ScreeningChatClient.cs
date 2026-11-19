using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.IntegrationTests.Ingest.Data;

/// <summary>
/// The triage chat client of the screening tests. It reads the chunk indexes from the user message and answers one
/// verdict for each: safe, unless the test marks the index unsafe. It records the indexes of each call, so a test sees
/// the batches, and it can run a hook inside the call.
/// </summary>
public sealed partial class ScreeningChatClient : IChatClient
{
    public const string UnsafeReason = "The chunk orders the reader to ignore its rules.";
    public const string SafeReason = "The chunk describes a machine.";
    public const int InputTokensPerCall = 50;
    public const int OutputTokensPerCall = 10;

    private readonly List<IReadOnlyList<int>> _batches = [];

    public IReadOnlyList<IReadOnlyList<int>> Batches => _batches;

    public HashSet<int> UnsafeIndexes { get; } = [];

    /// <summary>Runs inside each call before it answers.</summary>
    public Func<Task>? Before { get; set; }

    /// <summary>When set, every call throws it after it records the batch.</summary>
    public Exception? Failure { get; set; }

    /// <summary>The number of the first call that throws <see cref="Failure"/>. 0 means every call.</summary>
    public int FailFromCall { get; set; }

    public void Reset()
    {
        _batches.Clear();
        UnsafeIndexes.Clear();
        Before = null;
        Failure = null;
        FailFromCall = 0;
    }

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var text = string.Join(Environment.NewLine, messages.Select(message => message.Text));
        var indexes = ChunkIndexPattern()
            .Matches(text)
            .Select(match => int.Parse(match.Groups[groupnum: 1].Value))
            .ToList();
        _batches.Add(indexes);

        if (Failure is not null && (FailFromCall == 0 || _batches.Count >= FailFromCall))
        {
            throw Failure;
        }

        if (Before is not null)
        {
            await Before();
        }

        var verdicts = indexes.Select(index => new
        {
            index,
            isSafe = !UnsafeIndexes.Contains(index),
            reason = UnsafeIndexes.Contains(index) ? UnsafeReason : SafeReason,
        });
        var answer = JsonSerializer.Serialize(new { verdicts });

        return new ChatResponse(new ChatMessage(ChatRole.Assistant, answer))
        {
            Usage = new UsageDetails { InputTokenCount = InputTokensPerCall, OutputTokenCount = OutputTokensPerCall },
        };
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default(CancellationToken)
    ) => throw new NotSupportedException();

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    [GeneratedRegex(pattern: @"<chunk>\s*(\d+):")]
    private static partial Regex ChunkIndexPattern();
}
