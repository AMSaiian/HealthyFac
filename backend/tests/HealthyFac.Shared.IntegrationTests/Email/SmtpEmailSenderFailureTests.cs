using System.Diagnostics;
using System.Net;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Email;
using HealthyFac.Shared.IntegrationTests.Email.Data;
using HealthyFac.Testing.Logging;
using HealthyFac.Testing.Tracing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Shared.IntegrationTests.Email;

public sealed class SmtpEmailSenderFailureTests
{
    [Fact]
    public async Task SendAsync_WithUnresponsiveServer_FailsAfterConfiguredTimeout()
    {
        await using var server = new FakeSmtpServer(FakeSmtpBehavior.Silent);
        var sender = CreateSender(server.Port, new CapturingLogger<SmtpEmailSender>());
        var stopwatch = Stopwatch.StartNew();

        var act = () => sender.SendAsync(EmailTestData.Message, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        stopwatch.Elapsed.Should().BeLessThan(EmailTestData.TimeoutTolerance);
    }

    [Fact]
    public async Task SendAsync_WhenServerResetsConnectionOnQuit_Completes()
    {
        await using var server = new FakeSmtpServer(FakeSmtpBehavior.ResetOnQuit);
        var sender = CreateSender(server.Port, new CapturingLogger<SmtpEmailSender>());

        var act = () => sender.SendAsync(EmailTestData.Message, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendAsync_WhenServerRefuses_MarksSendSpanFailed()
    {
        using var capture = new SpanCapture(ObservabilityConstants.Sources.Email);
        using var parent = new Activity(EmailTestData.ParentName).Start();
        var sender = CreateSender(FakeSmtpServer.FindClosedPort(), new CapturingLogger<SmtpEmailSender>());

        var act = () => sender.SendAsync(EmailTestData.Message, CancellationToken.None);

        await act.Should().ThrowAsync<Exception>();
        var spans = capture.Spans.Where(span => span.TraceId == parent.TraceId).ToList();

        spans.Should().ContainSingle().Which.Status.Should().Be(ActivityStatusCode.Error);
    }

    private static SmtpEmailSender CreateSender(int port, ILogger<SmtpEmailSender> logger) =>
        new(
            Options.Create(
                new SmtpOptions
                {
                    Host = IPAddress.Loopback.ToString(),
                    Port = port,
                    Security = SmtpSecurity.None,
                    FromAddress = EmailTestData.FromAddress,
                    FromName = EmailTestData.FromName,
                    TimeoutSeconds = EmailTestData.ShortTimeoutSeconds,
                }),
            logger);
}
