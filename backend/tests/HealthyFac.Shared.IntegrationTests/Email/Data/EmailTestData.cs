using HealthyFac.Shared.Application.Email;

namespace HealthyFac.Shared.IntegrationTests.Email.Data;

public static class EmailTestData
{
    public const string FromAddress = "no-reply@healthyfac.test";
    public const string FromName = "HealthyFac Tests";
    public const string Recipient = "worker@example.test";
    public const string Subject = "A test subject";
    public const string TextBody = "The plain text body.";
    public const string HtmlBody = "<p>The <b>html</b> body.</p>";
    public const int ShortTimeoutSeconds = 1;
    public const string SpanRecipient = "span-check@example.test";
    public const string ParentName = "test-parent";

    public static readonly TimeSpan TimeoutTolerance = TimeSpan.FromSeconds(15);

    public static EmailMessage Message => MessageFor(Recipient);

    public static EmailMessage MessageFor(string recipient) => new()
    {
        To = recipient,
        Subject = Subject,
        TextBody = TextBody,
        HtmlBody = HtmlBody,
    };
}
