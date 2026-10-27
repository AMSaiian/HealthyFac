using HealthyFac.Shared.Application.Logging;

namespace HealthyFac.Shared.Application.Email;

public sealed record EmailMessage
{
    [SensitiveData]
    public required string To { get; init; }

    public required string Subject { get; init; }

    [SensitiveData]
    public required string TextBody { get; init; }

    [SensitiveData]
    public required string HtmlBody { get; init; }
}
