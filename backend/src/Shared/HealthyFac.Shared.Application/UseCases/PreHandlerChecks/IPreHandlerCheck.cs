namespace HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

/// <summary>
/// Checks the scope rule of one use case: the actor works inside their own company and departments. It never checks
/// permissions (endpoint policies do) and never checks other stateful rules (the handler does).
/// </summary>
/// <typeparam name="TRequest">The type of the request that the check covers.</typeparam>
public interface IPreHandlerCheck<in TRequest>
{
    /// <summary>
    /// Checks the scope of the actor against the request.
    /// </summary>
    /// <param name="request">The request to check.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation requests.</param>
    /// <returns>A task that completes when the request is inside the scope.</returns>
    /// <exception cref="ErrorHandling.ForbiddenAccessException">
    /// Thrown when the request names a company or a department outside the actor scope.
    /// </exception>
    /// <exception cref="ErrorHandling.NotFoundException">
    /// Thrown when the scope answer must not show that the resource exists.
    /// </exception>
    Task CheckAsync(TRequest request, CancellationToken cancellationToken);
}
