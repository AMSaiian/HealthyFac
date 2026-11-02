using System.Net;
using HealthyFac.Core.ApiTests.Employees.Data;
using HealthyFac.Core.ApiTests.Hosting;

namespace HealthyFac.Core.ApiTests.Employees;

public sealed class EmployeeAuthorizationTests(CoreApiFixture fixture) : IClassFixture<CoreApiFixture>
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task RetryProvisioning_WithOnlyInvitePermission_Returns403()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.InvitePermission] };

        var response = await _fixture.PostEmployeeActionAsync(
            EmployeeApiTestData.RetryProvisioningPath,
            claims,
            Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RetryProvisioning_WithManagePermission_ReachesUseCase()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.ManagePermission] };

        var response = await _fixture.PostEmployeeActionAsync(
            EmployeeApiTestData.RetryProvisioningPath,
            claims,
            Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deactivate_WithOnlyInvitePermission_Returns403()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.InvitePermission] };

        var response = await _fixture.PostEmployeeActionAsync(
            EmployeeApiTestData.DeactivatePath,
            claims,
            Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Deactivate_WithManagePermission_ReachesUseCase()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.ManagePermission] };

        var response = await _fixture.PostEmployeeActionAsync(
            EmployeeApiTestData.DeactivatePath,
            claims,
            Guid.CreateVersion7());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
