using HealthyFac.Core.IntegrationTests.Database.Data;
using HealthyFac.Core.IntegrationTests.Employees.Data;
using HealthyFac.Core.IntegrationTests.Shifts.Data;

namespace HealthyFac.Core.IntegrationTests.Employees;

[Collection(CoreEmployeesCollection.Name)]
public sealed class DeactivateRealtimeTests(CoreDatabaseFixture fixture)
{
    private readonly CoreDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task DeactivateEmployee_WithAnIdentityUser_RecordsOneSessionEndToThatUser()
    {
        var tenant = await _fixture.ArrangeShiftTenantAsync();
        _fixture.RealtimeQueue.Clear();

        await _fixture.DeactivateEmployeeAsync(tenant.CompanyId, tenant.EmployeeId, Guid.CreateVersion7());

        var push = _fixture.RealtimeQueue.Items.Should().ContainSingle().Subject.Push;
        push.IsSessionEnd.Should().BeTrue();
        push.UserId.Should().Be(tenant.IdentityUserId);
    }

    [Fact]
    public async Task DeactivateEmployee_WithoutAnIdentityUser_RecordsNothingAndSucceeds()
    {
        var tenant = await _fixture.ArrangeUnprovisionedShiftTenantAsync();
        _fixture.RealtimeQueue.Clear();

        await _fixture.DeactivateEmployeeAsync(tenant.CompanyId, tenant.EmployeeId, Guid.CreateVersion7());

        _fixture.RealtimeQueue.Items.Should().BeEmpty();
    }
}
