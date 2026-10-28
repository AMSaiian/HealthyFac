using FluentValidation;
using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class AcceptEmployeeInviteTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task AcceptAsync_WithValidToken_CreatesProvisioningEmployeeWithInviteData()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptValidEmail);

        var employeeId = await fixture.AcceptInviteAsync(arranged.Token);

        var employee = await fixture.LoadEmployeeAsync(employeeId);
        var invite = await fixture.LoadInviteAsync(arranged.InviteId);

        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        employee.Email.Should().Be(InviteTestData.AcceptValidEmail);
        employee.FirstName.Should().Be(InviteTestData.EmployeeFirstName);
        employee.LastName.Should().Be(InviteTestData.EmployeeLastName);
        employee.RoleId.Should().Be(invite.RoleId);
        employee.Departments.Select(department => department.Id).Should().BeEquivalentTo(arranged.DepartmentIds);

        invite.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptAsync_WithValidToken_AddsEmployeeAcceptedOutboxRow()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptOutboxEmail);

        var employeeId = await fixture.AcceptInviteAsync(arranged.Token);

        var messages = await ReadAcceptedMessagesAsync(employeeId);

        messages.Should().ContainSingle();
        messages[0].CompanyId.Should().Be(arranged.CompanyId);
        messages[0].Email.Should().Be(InviteTestData.AcceptOutboxEmail);
        messages[0].FirstName.Should().Be(InviteTestData.EmployeeFirstName);
        messages[0].LastName.Should().Be(InviteTestData.EmployeeLastName);
        messages[0].RoleCode.Should().Be(RoleCode.Worker.Value);
    }

    [Fact]
    public async Task AcceptAsync_SameTokenTwice_SecondCallThrowsGenericNotFound()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptTwiceEmail);
        await fixture.AcceptInviteAsync(arranged.Token);

        var act = () => fixture.AcceptInviteAsync(arranged.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task AcceptAsync_WithExpiredInvite_ThrowsGenericNotFound()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptExpiredEmail, DateTime.UtcNow.AddHours(-1));

        var act = () => fixture.AcceptInviteAsync(arranged.Token);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
    }

    [Fact]
    public async Task AcceptAsync_TwiceInParallel_CreatesOneEmployee()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptParallelEmail);
        using var race = fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => fixture.AcceptInviteAsync(arranged.Token)),
            race.RunAsync(() => fixture.AcceptInviteAsync(arranged.Token)));

        var employeeCount = await fixture.CountEmployeesAsync(
            InviteTestData.AcceptParallelEmail);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);

        employeeCount.Should().Be(1);
    }

    [Fact]
    public async Task AcceptAsync_WithEmailEmployedByOtherCompany_ThrowsConflict()
    {
        var first = await ArrangeAsync(InviteTestData.AcceptSharedEmail);
        var second = await ArrangeAsync(InviteTestData.AcceptSharedEmail);
        await fixture.AcceptInviteAsync(second.Token);

        var act = () => fixture.AcceptInviteAsync(first.Token);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
    }

    [Fact]
    public async Task AcceptAsync_SameEmailInTwoCompaniesInParallel_CreatesOneEmployee()
    {
        var first = await ArrangeAsync(InviteTestData.AcceptSharedParallelEmail);
        var second = await ArrangeAsync(InviteTestData.AcceptSharedParallelEmail);
        using var race = fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => fixture.AcceptInviteAsync(first.Token)),
            race.RunAsync(() => fixture.AcceptInviteAsync(second.Token)));

        var employeeCount = await fixture.CountEmployeesAsync(
            InviteTestData.AcceptSharedParallelEmail);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);

        employeeCount.Should().Be(1);
    }

    [Fact]
    public async Task AcceptAsync_WithBlankFirstName_ThrowsValidationException()
    {
        var arranged = await ArrangeAsync(InviteTestData.AcceptBlankNameEmail);

        var act = () => fixture.AcceptInviteAsync(arranged.Token, firstName: " ");

        await act.Should().ThrowAsync<ValidationException>();
    }

    private Task<ArrangedInvite> ArrangeAsync(string email, DateTime? expiresAt = null) =>
        fixture.ArrangeInviteAsync(
            email,
            expiresAt ?? DateTime.UtcNow.AddHours(1),
            InviteTestData.FirstDepartmentName,
            InviteTestData.SecondDepartmentName);

    private async Task<IReadOnlyList<EmployeeAccepted>> ReadAcceptedMessagesAsync(Guid employeeId)
    {
        var messages = await fixture.ReadOutboxMessagesAsync<EmployeeAccepted>();

        return messages.Where(message => message.EmployeeId == employeeId).ToList();
    }
}
