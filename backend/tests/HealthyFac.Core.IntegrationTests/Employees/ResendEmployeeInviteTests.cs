using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.Application.Employees.ResendEmployeeInvite;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Testing.Exceptions;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class ResendEmployeeInviteTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task ResendAsync_WithExpiredInvite_RenewsSaltAndExpiry()
    {
        var (companyId, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendExpiredEmail,
            DateTime.UtcNow.AddHours(-1));
        var oldInvite = await fixture.LoadInviteAsync(inviteId);

        await ResendAsync(companyId, inviteId);

        var invite = await fixture.LoadInviteAsync(inviteId);

        invite.ExpiresAt.Should()
            .BeCloseTo(
                DateTime.UtcNow.AddHours(InviteTestData.ExpectedLifetimeHours),
                TimeSpan.FromMinutes(1));
        invite.TokenSalt.Should().NotEqual(oldInvite.TokenSalt);
    }

    [Fact]
    public async Task ResendAsync_WithOpenInvite_AddsSecondEmployeeInvitedOutboxRow()
    {
        var (companyId, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendOutboxEmail,
            DateTime.UtcNow.AddHours(1));
        var countBefore = await CountInvitedMessagesAsync(inviteId);

        await ResendAsync(companyId, inviteId);

        var countAfter = await CountInvitedMessagesAsync(inviteId);

        countAfter.Should().Be(countBefore + 1);
    }

    [Fact]
    public async Task ResendAsync_WithOpenInvite_InvalidatesOldToken()
    {
        var (companyId, inviteId, oldToken) = await ArrangeInviteAsync(
            InviteTestData.ResendTokenEmail,
            DateTime.UtcNow.AddHours(1));

        await ResendAsync(companyId, inviteId);

        var invite = await fixture.LoadInviteAsync(inviteId);

        fixture.IsTokenValid(oldToken, invite.TokenSalt).Should().BeFalse();
    }

    [Fact]
    public async Task ResendAsync_WithAcceptedInvite_ThrowsConflict()
    {
        var (companyId, inviteId, token) = await ArrangeInviteAsync(
            InviteTestData.ResendAcceptedEmail,
            DateTime.UtcNow.AddHours(1));
        await fixture.AcceptInviteAsync(token);

        var act = () => ResendAsync(companyId, inviteId);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteAlreadyAccepted);
    }

    [Fact]
    public async Task ResendAsync_WithInviteOfOtherCompany_ThrowsNotFound()
    {
        var (_, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendOtherCompanyEmail,
            DateTime.UtcNow.AddHours(1));
        var otherCompanyId = await fixture.CreateCompanyAsync(InviteTestData.CompanyName);

        var act = () => ResendAsync(otherCompanyId, inviteId);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteNotFound);
    }

    [Fact]
    public async Task ResendAsync_TwiceInParallel_EachCallSucceedsOrConflicts()
    {
        var (companyId, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendParallelEmail,
            DateTime.UtcNow.AddHours(1));

        var failures = await Task.WhenAll(TryResendAsync(companyId, inviteId), TryResendAsync(companyId, inviteId));

        failures.Count(failure => failure is null).Should().BeGreaterThanOrEqualTo(1);
        failures.Where(failure => failure is not null)
            .Should()
            .AllSatisfy(failure => failure.Should()
                .BeOfType<ConflictException>()
                .Which.Message.Should()
                .Be(EmployeeInviteErrors.Rejections.InviteChanged));
    }

    private async Task<(Guid CompanyId, Guid InviteId, string Token)> ArrangeInviteAsync(
        string email,
        DateTime expiresAt
    )
    {
        var arranged = await fixture.ArrangeInviteAsync(
            email,
            expiresAt,
            InviteTestData.FirstDepartmentName);

        return (arranged.CompanyId, arranged.InviteId, arranged.Token);
    }

    private async Task<int> CountInvitedMessagesAsync(Guid inviteId)
    {
        var messages = await fixture.ReadOutboxMessagesAsync<EmployeeInvited>();

        return messages.Count(message => message.InviteId == inviteId);
    }

    private Task<Exception?> TryResendAsync(Guid companyId, Guid inviteId) =>
        ExceptionCapture.CaptureAsync(() => ResendAsync(companyId, inviteId));

    private async Task ResendAsync(Guid companyId, Guid inviteId)
    {
        using var scope = fixture.ScopeFactory.CreateScope();
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse>>();

        await handler.HandleAsync(new ResendEmployeeInviteRequest { CompanyId = companyId, InviteId = inviteId });
    }
}
