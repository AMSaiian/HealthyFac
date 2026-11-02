namespace HealthyFac.Shared.Infrastructure.HttpFailures;

public static class HttpFailureConstants
{
    /// <summary>
    /// The most bytes of an error body that <c>EnsureSuccessAsync</c> reads. A longer body gives no reason.
    /// </summary>
    public const int MaxBodyBytes = 4096;

    /// <summary>
    /// The most characters of a reason that <c>EnsureSuccessAsync</c> puts into the exception message.
    /// </summary>
    public const int MaxReasonLength = 2000;
}
