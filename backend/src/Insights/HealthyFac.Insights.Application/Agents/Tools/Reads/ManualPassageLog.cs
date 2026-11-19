namespace HealthyFac.Insights.Application.Agents.Tools.Reads;

/// <summary>
/// Remembers which manual chunks the calls of one run returned and in which call (its sequence in the run). A later
/// <c>search_manual</c> call of the run returns <c>repeatOf</c> with that sequence instead of the text again.
/// </summary>
public sealed class ManualPassageLog
{
    private readonly Dictionary<Guid, int> _sequences = [];

    /// <summary>Finds the call that returned the chunk.</summary>
    /// <param name="chunkId">The ID of the chunk.</param>
    /// <param name="sequence">The sequence of the call, when the chunk was returned before.</param>
    /// <returns><see langword="true"/> when an earlier call returned the chunk.</returns>
    public bool TryGetSequence(Guid chunkId, out int sequence) => _sequences.TryGetValue(chunkId, out sequence);

    /// <summary>Remembers the chunk with the call that returned it. The first call stays.</summary>
    /// <param name="chunkId">The ID of the chunk.</param>
    /// <param name="sequence">The sequence of the call.</param>
    public void Add(Guid chunkId, int sequence) => _sequences.TryAdd(chunkId, sequence);
}
