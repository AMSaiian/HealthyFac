namespace HealthyFac.Shared.Application.Email;

/// <summary>
/// Sends one email through the mail provider of the deployment. The host picks the implementation at registration
/// (see <c>AddSmtpEmail</c>), so a use case does not depend on a provider.
/// </summary>
public interface IEmailSender
{
    /// <summary>
    /// Sends the message with a text body and an HTML body. Sending is a durable step: after the provider accepts
    /// the message, a cancellation cannot undo it.
    /// </summary>
    /// <param name="message">The message to send.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe until the provider accepts the
    /// message.</param>
    /// <returns>A task that completes when the provider accepts the message.</returns>
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
