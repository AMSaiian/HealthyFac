namespace HealthyFac.Telemetry.Infrastructure.Intake.Mqtt;

/// <summary>
/// A time limit for the end of a session. When the limit passes, or the caller token is cancelled, the token of this
/// object ends and the handlers of the session stop (their processing token is cancelled).
/// </summary>
/// <remarks>
/// The limit uses the time provider, so a test with a fake clock can drive it. <see cref="IsTimedOut"/> tells the limit
/// from the caller token.
/// </remarks>
internal sealed class ProcessingTimeout : IDisposable
{
    private readonly CancellationTokenSource _timeout;
    private readonly CancellationTokenSource _linked;
    private readonly CancellationTokenRegistration _registration;

    public ProcessingTimeout(
        TimeSpan delay,
        TimeProvider timeProvider,
        CancellationToken cancellationToken,
        Action cancelProcessing
    )
    {
        _timeout = new CancellationTokenSource(delay, timeProvider);
        _linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _timeout.Token);
        _registration = _linked.Token.Register(cancelProcessing);
    }

    public CancellationToken Token => _linked.Token;

    public bool IsTimedOut => _timeout.IsCancellationRequested;

    public void Dispose()
    {
        _registration.Dispose();
        _linked.Dispose();
        _timeout.Dispose();
    }
}
