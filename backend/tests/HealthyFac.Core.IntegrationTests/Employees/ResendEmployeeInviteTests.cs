using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.SendEmployeeInviteEmail;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.DependencyInjection;
using HealthyFac.Core.Messaging.Employees;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class ResendEmployeeInviteTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task ResendAsync_WithExpiredInvite_RenewsSaltAndExpiry()
    {
        var (companyId, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendExpiredEmail,
            DateTime.UtcNow.AddHours(value: -1));
        var oldInvite = await _fixture.LoadInviteAsync(inviteId);

        await ResendAsync(companyId, inviteId);

        var invite = await _fixture.LoadInviteAsync(inviteId);

        invite.ExpiresAt.Should()
            .BeCloseTo(
                DateTime.UtcNow.AddHours(InviteTestData.ExpectedLifetimeHours),
                TimeSpan.FromMinutes(minutes: 1));
        invite.TokenSalt.Should().NotEqual(oldInvite.TokenSalt);
    }

    [Fact]
    public async Task ResendAsync_WithOpenInvite_AddsSecondEmployeeInvitedOutboxRow()
    {
        var (companyId, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendOutboxEmail,
            DateTime.UtcNow.AddHours(value: 1));
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
            DateTime.UtcNow.AddHours(value: 1));

        await ResendAsync(companyId, inviteId);

        var invite = await _fixture.LoadInviteAsync(inviteId);

        _fixture.IsTokenValid(oldToken, invite.TokenSalt).Should().BeFalse();
    }

    [Fact]
    public async Task ResendAsync_WithAcceptedInvite_ThrowsConflict()
    {
        var (companyId, inviteId, token) = await ArrangeInviteAsync(
            InviteTestData.ResendAcceptedEmail,
            DateTime.UtcNow.AddHours(value: 1));
        await _fixture.AcceptInviteAsync(token);

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
            DateTime.UtcNow.AddHours(value: 1));
        var otherCompanyId = await _fixture.CreateCompanyAsync(InviteTestData.CompanyName);

        var act = () => ResendAsync(otherCompanyId, inviteId);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteNotFound);
    }

    [Fact]
    public async Task ResendAsync_TwiceInParallel_SucceedsOnceAndConflictsOnce()
    {
        var (companyId, inviteId, _) = await ArrangeInviteAsync(
            InviteTestData.ResendParallelEmail,
            DateTime.UtcNow.AddHours(value: 1));
        var countBefore = await CountInvitedMessagesAsync(inviteId);
        using var race = _fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => ResendAsync(companyId, inviteId)),
            race.RunAsync(() => ResendAsync(companyId, inviteId)));

        var countAfter = await CountInvitedMessagesAsync(inviteId);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.InviteChanged);

        countAfter.Should().Be(countBefore + 1);
    }

    private async Task<(Guid CompanyId, Guid InviteId, string Token)> ArrangeInviteAsync(
        string email,
        DateTime expiresAt
    )
    {
        var arranged = await _fixture.ArrangeInviteAsync(
            email,
            expiresAt,
            InviteTestData.FirstDepartmentName);

        return (arranged.CompanyId, arranged.InviteId, arranged.Token);
    }

    private async Task<int> CountInvitedMessagesAsync(Guid inviteId)
    {
        var messages = await _fixture.ReadOutboxMessagesAsync<EmployeeInvited>();

        return messages.Count(message => message.InviteId == inviteId);
    }

    private async Task ResendAsync(Guid companyId, Guid inviteId)
    {
        using var scope = _fixture.CreateUserScope(companyId, []);
        var handler = scope.ServiceProvider
            .GetRequiredService<IRequestHandler<ResendEmployeeInviteRequest, ResendEmployeeInviteResponse>>();

        await handler.HandleAsync(new ResendEmployeeInviteRequest { CompanyId = companyId, InviteId = inviteId });
    }
}
