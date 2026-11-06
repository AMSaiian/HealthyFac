using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class RetryProvisioningTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task RetryProvisioning_ForFailedEmployee_SetsProvisioningAndAddsEmployeeAccepted()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.RetryFailedEmail);
        await _fixture.ChangeEmployeeAsync(arranged.EmployeeId, employee => employee.MarkProvisioningFailed());

        var response = await _fixture.RetryProvisioningAsync(arranged.CompanyId, arranged.EmployeeId);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await _fixture.ReadAcceptedMessagesAsync(arranged.EmployeeId);

        response.EmployeeId.Should().Be(arranged.EmployeeId);
        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        messages.Should().HaveCount(expected: 2);
        messages.Select(message => message.Email).Should().OnlyContain(email => email == employee.Email);
    }

    [Fact]
    public async Task RetryProvisioning_ForProvisioningEmployee_AddsEmployeeAccepted()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.RetryProvisioningEmail);

        await _fixture.RetryProvisioningAsync(arranged.CompanyId, arranged.EmployeeId);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await _fixture.ReadAcceptedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        employee.ProvisioningRetriedAt.Should().NotBeNull();
        messages.Should().HaveCount(expected: 2);
    }

    [Fact]
    public async Task RetryProvisioning_ConcurrentWithDeactivate_OneConflicts()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.RetryRaceEmail);
        using var race = _fixture.SaveGate.StartRace(InviteTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.RetryProvisioningAsync(arranged.CompanyId, arranged.EmployeeId)),
            race.RunAsync(() => _fixture.DeactivateEmployeeAsync(
                arranged.CompanyId,
                arranged.EmployeeId,
                Guid.CreateVersion7())));

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null)
            .Should()
            .BeOfType<ConflictException>()
            .Which.Message.Should()
            .Be(EmployeeProvisioningErrors.Rejections.EmployeeChanged);
    }

    [Fact]
    public async Task RetryProvisioning_ForActiveEmployee_ThrowsConflict()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.RetryActiveEmail);
        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, Guid.CreateVersion7());

        var act = () => _fixture.RetryProvisioningAsync(arranged.CompanyId, arranged.EmployeeId);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeProvisioningErrors.Rejections.EmployeeNotRetryable);
        var messages = await _fixture.ReadAcceptedMessagesAsync(arranged.EmployeeId);

        messages.Should().ContainSingle();
    }

    [Fact]
    public async Task RetryProvisioning_ForEmployeeOfOtherCompany_ThrowsNotFound()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.RetryOtherCompanyEmail);

        var act = () => _fixture.RetryProvisioningAsync(Guid.CreateVersion7(), arranged.EmployeeId);

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeProvisioningErrors.Rejections.EmployeeNotFound);
    }
}
