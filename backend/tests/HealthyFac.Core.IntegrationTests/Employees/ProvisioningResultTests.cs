using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class ProvisioningResultTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task IdentityProvisioned_ForProvisioningEmployee_ActivatesAndLinks()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteEmail);
        var identityUserId = Guid.CreateVersion7();

        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForActiveEmployeeWithSameIdentity_Completes()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteTwiceEmail);
        var identityUserId = Guid.CreateVersion7();
        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_WithOtherIdentity_Throws()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteOtherIdentityEmail);
        var identityUserId = Guid.CreateVersion7();
        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var act = () => _fixture.CompleteProvisioningAsync(arranged.EmployeeId, Guid.CreateVersion7());

        await act.Should().ThrowAsync<InvalidOperationException>();
        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForMissingEmployee_Throws()
    {
        var act = () => _fixture.CompleteProvisioningAsync(Guid.CreateVersion7(), Guid.CreateVersion7());

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForProvisioningEmployee_MarksFailed()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailEmail);

        await _fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.ProvisioningFailed);
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForFailedEmployee_Completes()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailTwiceEmail);
        await _fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        await _fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.ProvisioningFailed);
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForActiveEmployee_Completes()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailActiveEmail);
        var identityUserId = Guid.CreateVersion7();
        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await _fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForDeactivatedEmployee_LinksIdentityAndAddsEmployeeDeactivated()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.CompleteDeactivatedEmail);
        await _fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());
        var identityUserId = Guid.CreateVersion7();

        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await _fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
        employee.IdentityUserId.Should().Be(identityUserId);
        messages.Should().HaveCount(2);
        messages.Should().ContainSingle(message => message.IdentityUserId == identityUserId);
    }

    [Fact]
    public async Task IdentityProvisioned_ForDeactivatedEmployeeWithSameIdentity_Completes()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(
            ProvisioningTestData.CompleteDeactivatedTwiceEmail);
        await _fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());
        var identityUserId = Guid.CreateVersion7();
        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await _fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await _fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
        messages.Should().HaveCount(2);
    }

    [Fact]
    public async Task IdentityProvisioningFailed_ForDeactivatedEmployee_Completes()
    {
        var arranged = await _fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.FailDeactivatedEmail);
        await _fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());

        await _fixture.FailProvisioningAsync(arranged.EmployeeId, ProvisioningTestData.FailureReason);

        var employee = await _fixture.LoadEmployeeAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
    }
}
