namespace HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

/// <summary>
/// Labels a use case that has no scope rule. A use case declares an empty sealed subclass, so the missing rule is a
/// visible choice and <c>AddUseCases</c> still finds one pre-handler check for every request.
/// </summary>
/// <typeparam name="TRequest">The type of the request that the check allows.</typeparam>
public class PassThroughPreHandlerCheck<TRequest> : IPreHandlerCheck<TRequest>
{
    public Task CheckAsync(TRequest request, CancellationToken cancellationToken) => Task.CompletedTask;
}
