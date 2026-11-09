using Microsoft.Extensions.Logging;

namespace HealthyFac.Shared.Application.ErrorHandling;

/// <summary>
/// Reports that a dependency of the service is down, so the request cannot finish. The web layer answers with status
/// 503. The client may retry later.
/// </summary>
public class ServiceUnavailableException : Exception, IRejection
{
    public ServiceUnavailableException()
        : base()
    {
    }

    public ServiceUnavailableException(string message)
        : base(message)
    {
    }

    public ServiceUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LogLevel? Level { get; init; } = LogLevel.Warning;
}
