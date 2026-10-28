using HealthyFac.Shared.Application.Email;

namespace HealthyFac.Core.ApiTests.Hosting;

public sealed class DiscardingEmailSender : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken) => Task.CompletedTask;
}
