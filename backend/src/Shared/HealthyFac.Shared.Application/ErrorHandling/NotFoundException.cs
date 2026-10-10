using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

public class NotFoundException : Exception, IRejection
{
    public NotFoundException()
        : base()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LogLevel? Level { get; init; } = LogLevel.Information;
}
