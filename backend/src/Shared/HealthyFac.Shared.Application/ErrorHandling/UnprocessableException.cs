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
}
