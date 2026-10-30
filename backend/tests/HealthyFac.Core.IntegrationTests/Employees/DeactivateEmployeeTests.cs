using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Core.IntegrationTests.Employees;

public sealed class DeactivateEmployeeTests(CoreDatabaseFixture fixture) : IClassFixture<CoreDatabaseFixture>
{
    [Fact]
    public async Task Deactivate_ActiveEmployee_SetsDeactivatedAndAddsMessage()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.DeactivateEmail);
        var identityUserId = Guid.CreateVersion7();
        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        await fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());

        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Deactivated);
        messages.Should().ContainSingle();
        messages[0].CompanyId.Should().Be(arranged.CompanyId);
        messages[0].IdentityUserId.Should().Be(identityUserId);
        messages[0].DeactivatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Deactivate_ProvisioningEmployee_AddsMessageWithoutIdentity()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.DeactivateProvisioningEmail);

        await fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());

        var messages = await fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        messages.Should().ContainSingle().Which.IdentityUserId.Should().BeNull();
    }

    [Fact]
    public async Task Deactivate_Self_ThrowsConflict()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.DeactivateSelfEmail);
        var identityUserId = Guid.CreateVersion7();
        await fixture.CompleteProvisioningAsync(arranged.EmployeeId, identityUserId);

        var act = () => fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, identityUserId);

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeProvisioningErrors.Rejections.EmployeeIsSelf);
        var employee = await fixture.LoadEmployeeAsync(arranged.EmployeeId);
        var messages = await fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        messages.Should().BeEmpty();
    }

    [Fact]
    public async Task Deactivate_EmployeeOfOtherCompany_ThrowsNotFound()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.DeactivateOtherCompanyEmail);

        var act = () => fixture.DeactivateEmployeeAsync(
            Guid.CreateVersion7(),
            arranged.EmployeeId,
            Guid.CreateVersion7());

        await act.Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage(EmployeeProvisioningErrors.Rejections.EmployeeNotFound);
    }

    [Fact]
    public async Task Deactivate_AlreadyDeactivatedEmployee_ThrowsConflict()
    {
        var arranged = await fixture.ArrangeProvisioningEmployeeAsync(ProvisioningTestData.DeactivateTwiceEmail);
        await fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());

        var act = () => fixture.DeactivateEmployeeAsync(arranged.CompanyId, arranged.EmployeeId, Guid.CreateVersion7());

        await act.Should()
            .ThrowAsync<ConflictException>()
            .WithMessage(EmployeeProvisioningErrors.Rejections.EmployeeAlreadyDeactivated);
        var messages = await fixture.ReadDeactivatedMessagesAsync(arranged.EmployeeId);

        messages.Should().ContainSingle();
    }
}
