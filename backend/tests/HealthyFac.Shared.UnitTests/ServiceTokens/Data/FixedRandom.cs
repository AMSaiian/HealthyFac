namespace HealthyFac.Shared.UnitTests.ServiceTokens.Data;

/// <summary>
/// Returns one fixed value from <see cref="NextDouble"/>, so a test controls the refresh jitter of a token.
/// </summary>
public sealed class FixedRandom : Random
{
    private readonly double _value;

    /// <summary>
    /// Creates a source that always returns <paramref name="value"/>.
    /// </summary>
    /// <param name="value">The fraction of the maximum jitter, from 0 to 1.</param>
    public FixedRandom(double value)
    {
        _value = value;
    }

    public override double NextDouble() => _value;
}
