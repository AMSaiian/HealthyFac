using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

public class ForbiddenAccessException : Exception, IRejection
{
    public ForbiddenAccessException()
        : base()
    {
    }

    public ForbiddenAccessException(string message)
        : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LogLevel? Level { get; init; } = LogLevel.Warning;
}
