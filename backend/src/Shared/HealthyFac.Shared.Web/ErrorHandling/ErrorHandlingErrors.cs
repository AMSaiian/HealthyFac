namespace HealthyFac.Shared.Web.ErrorHandling;

/// <summary>
/// Defines the exceptions that the error handling pipeline throws itself.
/// </summary>
public static class ErrorHandlingErrors
{
    /// <summary>
    /// Reports a builder invoked with an exception it cannot read. The dictionary key and the cast are
    /// declared together, so this signals a wiring mistake rather than a runtime condition.
    /// </summary>
    /// <param name="expected">The type the builder expects.</param>
    /// <param name="actual">The type it received.</param>
    /// <returns>The exception to throw.</returns>
    public static NotSupportedException UnsupportedExceptionType(Type expected, Type actual) =>
        new($"The problem details builder for {expected.Name} received {actual.Name}.");
}
