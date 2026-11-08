using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class AcceptEmployeeInviteTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task AcceptAsync_WithValidToken_CreatesProvisioningEmployeeWithInviteData()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptValidEmail);

        var employeeId = await _fixture.AcceptInviteAsync(arranged.Token);

        var employee = await _fixture.LoadEmployeeAsync(employeeId);
        var invite = await _fixture.LoadInviteAsync(arranged.InviteId);

        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        employee.Email.Should().Be(InviteTestData.AcceptValidEmail);
        employee.FirstName.Should().Be(InviteTestData.EmployeeFirstName);
        employee.LastName.Should().Be(InviteTestData.EmployeeLastName);
        employee.RoleId.Should().Be(invite.RoleId);

        invite.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptAsync_WithValidToken_AddsEmployeeAcceptedOutboxRow()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptOutboxEmail);

        var employeeId = await _fixture.AcceptInviteAsync(arranged.Token);

        var messages = await ReadAcceptedMessagesAsync(employeeId);

        messages.Should().ContainSingle();
        messages[index: 0].CompanyId.Should().Be(arranged.CompanyId);
        messages[index: 0].Email.Should().Be(InviteTestData.AcceptOutboxEmail);
        messages[index: 0].FirstName.Should().Be(InviteTestData.EmployeeFirstName);
        messages[index: 0].LastName.Should().Be(InviteTestData.EmployeeLastName);
        messages[index: 0].RoleCode.Should().Be(RoleCode.Worker.Value);
    }

    [Fact]
    public async Task AcceptAsync_SameTokenTwice_SecondCallThrowsGenericNotFound()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptTwiceEmail);
        await _fixture.AcceptInviteAsync(arranged.Token);

        var act = () => _fixture.AcceptInviteAsync(arranged.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task AcceptAsync_WithExpiredInvite_ThrowsGenericNotFound()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptExpiredEmail, DateTime.UtcNow.AddHours(value: -1));

        var act = () => _fixture.AcceptInviteAsync(arranged.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task AcceptAsync_TwiceInParallel_CreatesOneEmployee()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptParallelEmail);
        using var race = _fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.AcceptInviteAsync(arranged.Token)),
            race.RunAsync(() => _fixture.AcceptInviteAsync(arranged.Token)));

        var employeeCount = await _fixture.CountEmployeesAsync(
            InviteTestData.AcceptParallelEmail);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);

        employeeCount.Should().Be(expected: 1);
    }

    [Fact]
    public async Task AcceptAsync_WithEmailEmployedByOtherCompany_ThrowsConflict()
    {
        var first = await ArrangeAsync(InviteTestData.AcceptSharedEmail);
        var second = await ArrangeAsync(InviteTestData.AcceptSharedEmail);
        await _fixture.AcceptInviteAsync(second.Token);

        var act = () => _fixture.AcceptInviteAsync(first.Token);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
    }

    [Fact]
    public async Task AcceptAsync_SameEmailInTwoCompaniesInParallel_CreatesOneEmployee()
    {
        var first = await ArrangeAsync(InviteTestData.AcceptSharedParallelEmail);
        var second = await ArrangeAsync(InviteTestData.AcceptSharedParallelEmail);
        using var race = _fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.AcceptInviteAsync(first.Token)),
            race.RunAsync(() => _fixture.AcceptInviteAsync(second.Token)));

        var employeeCount = await _fixture.CountEmployeesAsync(
            InviteTestData.AcceptSharedParallelEmail);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);

        employeeCount.Should().Be(expected: 1);
    }

    private Task<ArrangedInvite> ArrangeAsync(string email, DateTime? expiresAt = null) =>
        _fixture.ArrangeInviteAsync(
            email,
            expiresAt ?? DateTime.UtcNow.AddHours(value: 1));

    private async Task<IReadOnlyList<EmployeeAccepted>> ReadAcceptedMessagesAsync(Guid employeeId)
    {
        var messages = await _fixture.ReadOutboxMessagesAsync<EmployeeAccepted>();

        return messages.Where(message => message.EmployeeId == employeeId).ToList();
    }
}
