using HealthyFac.Shared.Application.Email;
using HealthyFac.Shared.Application.Observability;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace HealthyFac.Shared.Infrastructure.Email;

/// <summary>
/// Sends email through an SMTP server with MailKit. It opens one connection for each message, so it holds no state
/// between calls.
/// </summary>
/// <remarks>
/// The send is the durable step. The disconnect after it ignores cancellation, and a disconnect failure only writes
/// a warning: the server already accepted the message, so a thrown exception would make the caller retry and send
/// a second email. The connection closes on disposal anyway. The connect, authenticate, and send calls keep
/// propagating their failures. The whole send writes one <c>Email.Send</c> span, which holds the MailKit spans of the
/// connect, authenticate, send, and disconnect steps. The span is successful once the server accepted the message,
/// so a disconnect failure does not change it. The span holds no recipient, subject, or body. Each SMTP operation
/// has the timeout <see cref="SmtpOptions.TimeoutSeconds"/>, because the send runs inside the inbox transaction,
/// and a stalled server holds the row lock until the timeout.
/// </remarks>
/// <param name="options">The validated SMTP settings.</param>
/// <param name="logger">Logs a failed disconnect.</param>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken)
    {
        var smtp = options.Value;
        var mimeMessage = CreateMimeMessage(smtp, message);

        using var activity = EmailActivitySource.Instance.StartActivity(ObservabilityConstants.Spans.EmailSend);

        try
        {
            using var client = new SmtpClient();
            client.Timeout = (int)TimeSpan.FromSeconds(smtp.TimeoutSeconds).TotalMilliseconds;
            await client.ConnectAsync(smtp.Host, smtp.Port, MapSecurity(smtp.Security), cancellationToken);
            if (!string.IsNullOrWhiteSpace(smtp.UserName) && smtp.Password is { } password)
            {
                await client.AuthenticateAsync(smtp.UserName, password, cancellationToken);
            }

            await client.SendAsync(mimeMessage, cancellationToken);
            activity.MarkSuccess();
            await DisconnectAsync(client);
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            activity.MarkException(exception);

            throw;
        }
    }

    private async Task DisconnectAsync(SmtpClient client)
    {
        try
        {
            await client.DisconnectAsync(true, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "The SMTP disconnect failed after the server accepted the message.");
        }
    }

    private static MimeMessage CreateMimeMessage(SmtpOptions smtp, EmailMessage message)
    {
        var body = new BodyBuilder { TextBody = message.TextBody, HtmlBody = message.HtmlBody };
        var mimeMessage = new MimeMessage { Subject = message.Subject, Body = body.ToMessageBody() };
        mimeMessage.From.Add(new MailboxAddress(smtp.FromName, smtp.FromAddress));
        mimeMessage.To.Add(MailboxAddress.Parse(message.To));

        return mimeMessage;
    }

    private static SecureSocketOptions MapSecurity(SmtpSecurity security) => security switch
    {
        SmtpSecurity.None => SecureSocketOptions.None,
        SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
        SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
        _ => throw new ArgumentOutOfRangeException(nameof(security), security, null),
    };
}
