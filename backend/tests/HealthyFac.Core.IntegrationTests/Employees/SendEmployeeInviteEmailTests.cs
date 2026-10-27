using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class SendEmployeeInviteEmailTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task Handle_WithOpenInvite_SendsOneEmailWithValidLink()
    {
        var arranged = await ArrangeOpenInviteAsync(InviteEmailTestData.OpenInviteEmail);
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<IRequestHandler<EmployeeInvited>>();

        await handler.HandleAsync(ToMessage(arranged, InviteEmailTestData.OpenInviteEmail), CancellationToken.None);

        var emails = fixture.ReadSentEmails(InviteEmailTestData.OpenInviteEmail);
        var invite = await fixture.LoadInviteAsync(arranged.InviteId);

        emails.Should().ContainSingle();
        emails[0].TextBody.Should().Contain($"{InviteEmailTestData.ConfiguredSpaBaseUrl}/invite#token=");
        fixture.IsTokenValid(emails[0].ReadLinkToken(), invite.TokenSalt).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AfterRenew_SendsLinkValidForCurrentSalt()
    {
        var arranged = await ArrangeOpenInviteAsync(InviteEmailTestData.RenewedInviteEmail);
        var message = ToMessage(arranged, InviteEmailTestData.RenewedInviteEmail);
        await fixture.RenewInviteAsync(arranged.InviteId);
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = fixture.CreateInviteEmailHandler(scope);

        await handler.HandleAsync(message, CancellationToken.None);

        var emails = fixture.ReadSentEmails(InviteEmailTestData.RenewedInviteEmail);
        var invite = await fixture.LoadInviteAsync(arranged.InviteId);

        emails.Should().ContainSingle();
        fixture.IsTokenValid(emails[0].ReadLinkToken(), invite.TokenSalt).Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithAcceptedInvite_SendsNothingAndCompletes()
    {
        var arranged = await ArrangeOpenInviteAsync(InviteEmailTestData.AcceptedInviteEmail);
        await fixture.AcceptInviteAsync(arranged.Token);
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = fixture.CreateInviteEmailHandler(scope);

        var act = () => handler.HandleAsync(
            ToMessage(arranged, InviteEmailTestData.AcceptedInviteEmail),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        fixture.ReadSentEmails(InviteEmailTestData.AcceptedInviteEmail).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithUnknownInvite_SendsNothingAndCompletes()
    {
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = fixture.CreateInviteEmailHandler(scope);
        var message = Guid.CreateVersion7()
            .ToInvitedMessage(Guid.CreateVersion7(), InviteEmailTestData.UnknownInviteEmail);

        var act = () => handler.HandleAsync(message, CancellationToken.None);

        await act.Should().NotThrowAsync();
        fixture.ReadSentEmails(InviteEmailTestData.UnknownInviteEmail).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithExpiredOpenInvite_SendsNothingAndCompletes()
    {
        var arranged = await fixture.ArrangeInviteAsync(
            InviteEmailTestData.ExpiredInviteEmail,
            DateTime.UtcNow.AddHours(-1),
            InviteTestData.FirstDepartmentName);
        var logger = new CapturingLogger<SendEmployeeInviteEmailRequestHandler>();
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = fixture.CreateInviteEmailHandler(scope, logger: logger);

        var act = () => handler.HandleAsync(
            ToMessage(arranged, InviteEmailTestData.ExpiredInviteEmail),
            CancellationToken.None);

        await act.Should().NotThrowAsync();
        fixture.ReadSentEmails(InviteEmailTestData.ExpiredInviteEmail).Should().BeEmpty();
        logger.Entries.Should().ContainSingle(entry => entry.Level == LogLevel.Information);
    }

    [Fact]
    public async Task Handle_WhenSenderThrows_Propagates()
    {
        var arranged = await ArrangeOpenInviteAsync(InviteEmailTestData.SenderFailureEmail);
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = fixture.CreateInviteEmailHandler(
            scope,
            new ThrowingEmailSender(new InvalidOperationException(InviteEmailTestData.SenderFailureMessage)));

        var act = () => handler.HandleAsync(
            ToMessage(arranged, InviteEmailTestData.SenderFailureEmail),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(InviteEmailTestData.SenderFailureMessage);
    }

    [Fact]
    public async Task Handle_WithTrailingSlashBaseUrl_BuildsSingleSlashLink()
    {
        var arranged = await ArrangeOpenInviteAsync(InviteEmailTestData.TrailingSlashEmail);
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = fixture.CreateInviteEmailHandler(
            scope,
            spaBaseUrl: InviteEmailTestData.TrailingSlashSpaBaseUrl);

        await handler.HandleAsync(
            ToMessage(arranged, InviteEmailTestData.TrailingSlashEmail),
            CancellationToken.None);

        var emails = fixture.ReadSentEmails(InviteEmailTestData.TrailingSlashEmail);

        emails.Should().ContainSingle();
        emails[0].TextBody.Should().Contain(InviteEmailTestData.TrailingSlashExpectedPrefix);
        emails[0].TextBody.Should().NotContain("//invite");
    }

    private Task<ArrangedInvite> ArrangeOpenInviteAsync(string email) => fixture.ArrangeInviteAsync(
        email,
        DateTime.UtcNow.AddHours(1),
        InviteTestData.FirstDepartmentName);

    private static EmployeeInvited ToMessage(ArrangedInvite arranged, string email) =>
        arranged.InviteId.ToInvitedMessage(arranged.CompanyId, email);
}
