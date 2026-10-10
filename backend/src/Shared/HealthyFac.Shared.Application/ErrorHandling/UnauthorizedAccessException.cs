using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

public class UnauthorizedAccessException : Exception, IRejection
{
    public UnauthorizedAccessException()
        : base()
    {
    }

    public UnauthorizedAccessException(string message)
        : base(message)
    {
    }

    public UnauthorizedAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LogLevel? Level { get; init; } = LogLevel.Warning;
}
