using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class ProvisioningResultTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task IdentityProvisioned_ForProvisioningEmployee_ActivatesAndLinks()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteEmail);
        var identityUserId = Guid.CreateVersion7();

        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForActiveEmployeeWithSameIdentity_Completes()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteTwiceEmail);
        var identityUserId = Guid.CreateVersion7();
        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_WithOtherIdentity_Throws()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteOtherIdentityEmail);
        var identityUserId = Guid.CreateVersion7();
        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var act = () => fixture.CompleteProvisioningAsync(arranged.EmployeeId, Guid.CreateVersion7());

        await act.Should().ThrowAsync<InvalidOperationException>();
        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForMissingEmployee_Throws()
    {
        var act = () => fixture.CompleteProvisioningAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForProvisioningEmployee_MarksFailed()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailEmail);

        await fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.ProvisioningFailed);
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForFailedEmployee_Completes()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailTwiceEmail);
        await fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        await fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.ProvisioningFailed);
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForActiveEmployee_Completes()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailActiveEmail);
        var identityUserId = Guid.CreateVersion7();
        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForDeactivatedEmployee_LinksIdentityAndAddsEmployeeDeactivated()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteDeactivatedEmail);
        await fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());
        var identityUserId = Guid.CreateVersion7();

        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
        employee.IdentityUserId.Should().Be(identityUserId);
        messages.Should().HaveCount(2);
        messages.Should().ContainSingle(message => message.IdentityUserId == identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForDeactivatedEmployeeWithSameIdentity_Completes()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(
            ProvisioningTestData.CompleteDeactivatedTwiceEmail);
        await fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());
        var identityUserId = Guid.CreateVersion7();
        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForDeactivatedEmployee_Completes()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailDeactivatedEmail);
        await fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());

        await fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
    }
}
