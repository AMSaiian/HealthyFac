namespace HealthyFac.Gateway.Web.Composition;

/// <summary>
/// Thrown when a service answered with data that the Gateway cannot compose (for example a sort value of another JSON
/// kind). The exception handler maps it to <c>503</c>, because the fault is in a service, not in the request.
/// </summary>
public sealed class CompositionException(string message, Exception? innerException = null)
    : Exception(message, innerException);
