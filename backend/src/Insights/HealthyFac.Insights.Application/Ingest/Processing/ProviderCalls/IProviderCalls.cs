namespace HealthyFac.Insights.Application.Ingest.Processing;

/// <summary>
/// Runs a call to an external service (Document, blob storage, Docling, Telemetry, Gemini) with retries for transient
/// errors.
/// </summary>
public interface IProviderCalls
{
    /// <summary>
    /// Runs the call and retries it after a transient error with an exponentially growing delay. A call is not
    /// transient when it fails with another error, and that error passes through unchanged.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the call.</typeparam>
    /// <param name="call">The call. It must be safe to repeat.</param>
    /// <param name="cancellationToken">The token that cancels the call and the waits.</param>
    /// <returns>The result of the first call that succeeds.</returns>
    /// <exception cref="ProviderUnavailableException">Thrown when every try ended with a transient error.</exception>
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> call,
        CancellationToken cancellationToken
    );
}
