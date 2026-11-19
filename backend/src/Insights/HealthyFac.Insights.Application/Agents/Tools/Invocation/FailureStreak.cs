namespace HealthyFac.Insights.Application.Agents.Tools.Invocation;

/// <summary>
/// Counts the tool calls of a run that failed or were rejected one after another. A successful call resets the count.
/// The tool function does not throw for a rejection, so the function loop cannot count them. The count reaching the
/// limit tells the caller that the model cannot make a valid call.
/// </summary>
/// <param name="limit">The number of failed calls in a row that exceeds the run.</param>
public sealed class FailureStreak(int limit)
{
    private readonly int _limit = limit;
    private int _count;

    /// <summary>Tells whether the failed calls in a row reached the limit.</summary>
    public bool IsExceeded => _count >= _limit;

    /// <summary>
    /// Records the outcome of one call.
    /// </summary>
    /// <param name="isFailure"><see langword="true"/> when the call failed or was rejected.</param>
    public void Record(bool isFailure) => _count = isFailure ? _count + 1 : 0;
}
