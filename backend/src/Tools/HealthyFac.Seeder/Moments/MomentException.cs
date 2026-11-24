namespace HealthyFac.Seeder.Moments;

/// <summary>
/// A relative moment that has a wrong form, or that falls on a local time that does not exist or is ambiguous.
/// </summary>
public sealed class MomentException : Exception
{
    public MomentException(string message)
        : base(message)
    {
    }
}
