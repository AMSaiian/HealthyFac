namespace HealthyFac.Shared.Infrastructure.Email;

public sealed record SmtpOptions
{
    public const string SectionName = "Smtp";

    public required string Host { get; init; }

    public int Port { get; init; } = EmailConstants.DefaultPort;

    public int TimeoutSeconds { get; init; } = EmailConstants.Timeout.DefaultSeconds;

    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    public string? UserName { get; init; }

    public string? Password { get; init; }

    public required string FromAddress { get; init; }

    public required string FromName { get; init; }
}
