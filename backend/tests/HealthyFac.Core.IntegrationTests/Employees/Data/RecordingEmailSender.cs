using System.Collections.Concurrent;
using HealthyFac.Shared.Application.Email;

namespace HealthyFac.Core.IntegrationTests.Employees.Data;

/// <summary>
/// Keeps every email in memory, so a test reads what the code under test sent without an SMTP server.
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentQueue<EmailMessage> _messages = new();

    public IReadOnlyCollection<EmailMessage> Messages => _messages;

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        _messages.Enqueue(message);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Fails every send with the exception that the test gives, like an SMTP server that is down.
/// </summary>
/// <param name="failure">The exception to throw.</param>
public sealed class ThrowingEmailSender(Exception failure) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => throw failure;
}
