namespace HealthyFac.Insights.Application.Agents;

/// <summary>
/// Runs a call of an agent run to an external service (the model, a tool backend) with the retries of the agent
/// settings.
/// </summary>
public interface IAgentProviderCalls
{
    /// <summary>
    /// Runs the call and retries it after a transient error, with the delays of
    /// <see cref="AgentOptions.ProviderRetryDelaysSeconds"/>. A permanent error ends the call at once.
    /// </summary>
    /// <typeparam name="TResult">The type of the result of the call.</typeparam>
    /// <param name="call">The call. It must be safe to repeat.</param>
    /// <param name="cancellationToken">The token that cancels the call and the waits.</param>
    /// <returns>The result of the first call that succeeds.</returns>
    /// <exception cref="Ingest.Processing.ProviderUnavailableException">
    /// Thrown when every try ended with a transient error, and for a permanent error at once.
    /// </exception>
    Task<TResult> ExecuteAsync<TResult>(
        Func<CancellationToken, Task<TResult>> call,
        CancellationToken cancellationToken
    );
}
