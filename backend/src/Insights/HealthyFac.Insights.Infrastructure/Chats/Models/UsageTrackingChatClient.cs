using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace HealthyFac.Insights.Infrastructure.Chats.Models;

/// <summary>
/// Adds the token usage of each model call of a stream to one total. A turn with tools makes several model calls, and
/// each call reports its own usage, so the largest count of the stream alone would count only one call.
/// </summary>
/// <param name="inner">The chat client that calls the provider.</param>
/// <param name="totals">Receives the usage of each call.</param>
public sealed class UsageTrackingChatClient(IChatClient inner, UsageTotals totals) : DelegatingChatClient(inner)
{
    private readonly UsageTotals _totals = totals;

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        long input = 0;
        long output = 0;

        try
        {
            await foreach (var update in base.GetStreamingResponseAsync(messages, options, cancellationToken))
            {
                var details = update.Contents.OfType<UsageContent>().LastOrDefault()?.Details;
                input = details?.InputTokenCount ?? input;
                output = details?.OutputTokenCount ?? output;

                yield return update;
            }
        }
        finally
        {
            _totals.Add(input, output);
        }
    }
}

/// <summary>
/// The token counts of the model calls of one turn.
/// </summary>
public sealed class UsageTotals
{
    public long Input { get; private set; }

    public long Output { get; private set; }

    /// <summary>
    /// Adds the counts of one model call.
    /// </summary>
    /// <param name="input">The input tokens of the call.</param>
    /// <param name="output">The output tokens of the call.</param>
    public void Add(long input, long output)
    {
        Input += input;
        Output += output;
    }
}
