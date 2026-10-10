using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

public class ConflictException : Exception, IRejection
{
    public ConflictException()
        : base()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LogLevel? Level { get; init; } = LogLevel.Information;
}
