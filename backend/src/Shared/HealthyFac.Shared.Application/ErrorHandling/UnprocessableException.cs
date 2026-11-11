using System.Collections.Frozen;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

public class UnprocessableException : Exception, IRejection
{
    public UnprocessableException()
        : base()
    {
    }

    public UnprocessableException(string message)
        : base(message)
    {
    }

    public UnprocessableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LogLevel? Level { get; init; } = LogLevel.Information;

    /// <summary>
    /// Extra members that the response adds to the problem details (e.g., the list of affected work orders).
    /// </summary>
    public IReadOnlyDictionary<string, object?> Extensions { get; init; } = FrozenDictionary<string, object?>.Empty;
}
