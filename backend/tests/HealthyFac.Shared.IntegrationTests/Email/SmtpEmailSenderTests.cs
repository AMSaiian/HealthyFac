using System.Diagnostics;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.IntegrationTests.Email.Data;
using HealthyFac.Testing.Tracing;

namespace HealthyFac.Shared.IntegrationTests.Email;

public sealed class SmtpEmailSenderTests(MailpitFixture fixture) : IClassFixture<MailpitFixture>
{
    [Fact]
    public async Task SendAsync_WithMailpit_WritesSendSpanWithMailKitChildren()
    {
        using var capture = new SpanCapture(
            ObservabilityConstants.Sources.Email,
            MailKit.Telemetry.SmtpClient.ActivitySourceName);
        using var parent = new Activity(EmailTestData.ParentName).Start();

        await fixture.EmailSender.SendAsync(
            EmailTestData.MessageFor(EmailTestData.SpanRecipient),
            CancellationToken.None);

        var spans = capture.Spans.Where(span => span.TraceId == parent.TraceId).ToList();
        var send = spans.Should()
            .ContainSingle(span => span.OperationName == ObservabilityConstants.Spans.EmailSend)
            .Subject;
        var children = spans.Where(span => span.ParentId == send.Id).ToList();
        var tagValues = spans
            .SelectMany(span => span.TagObjects)
            .Select(tag => tag.Value?.ToString() ?? string.Empty)
            .ToList();

        send.Status.Should().Be(ActivityStatusCode.Ok);
        children.Should().NotBeEmpty();
        children.Should().OnlyContain(span => span.Source.Name == MailKit.Telemetry.SmtpClient.ActivitySourceName);
        tagValues.Should().NotContain(value => value.Contains(EmailTestData.SpanRecipient));
    }

    [Fact]
    public async Task SendAsync_WithMessage_DeliversToMailpitWithBothBodies()
    {
        await fixture.EmailSender.SendAsync(EmailTestData.Message, CancellationToken.None);

        var messages = await fixture.Server.GetMessagesAsync();
        var message = messages.Should().ContainSingle(stored => stored.To.Contains(EmailTestData.Recipient)).Subject;
        var text = await fixture.Server.GetMessageTextAsync(message.Id);
        var html = await fixture.Server.GetMessageHtmlAsync(message.Id);

        message.To.Should().ContainSingle().Which.Should().Be(EmailTestData.Recipient);
        message.Subject.Should().Be(EmailTestData.Subject);
        text.Should().Contain(EmailTestData.TextBody);
        html.Should().Contain(EmailTestData.HtmlBody);
    }
}
