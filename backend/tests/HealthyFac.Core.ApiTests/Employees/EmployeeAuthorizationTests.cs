using System.Net;
using HealthyFac.Core.ApiTests.Employees.Data;
using HealthyFac.Core.ApiTests.Hosting;

namespace HealthyFac.Core.ApiTests.Employees;

[Collection(CoreApiCollection.Name)]
public sealed class EmployeeAuthorizationTests(CoreApiFixture fixture)
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

    [Fact]
    public async Task Search_WithoutReadPermission_Returns403()
    {
        var claims = _fixture.CreateSupervisorClaims() with
        {
            Permissions = [EmployeeApiTestData.MachinesReadPermission],
        };

        var response = await _fixture.PostEmployeeSearchAsync(EmployeeApiTestData.SearchPath, claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_WithReadPermission_Returns200()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.ReadPermission] };

        var response = await _fixture.PostEmployeeSearchAsync(EmployeeApiTestData.SearchPath, claims);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Lookup_WithoutReadPermissionReturns403AndWithItReturns200()
    {
        var withoutRead = _fixture.CreateSupervisorClaims() with
        {
            Permissions = [EmployeeApiTestData.MachinesReadPermission],
        };
        var withRead = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.ReadPermission] };

        var forbidden = await _fixture.PostEmployeeSearchAsync(EmployeeApiTestData.LookupPath, withoutRead);
        var allowed = await _fixture.PostEmployeeSearchAsync(EmployeeApiTestData.LookupPath, withRead);

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InviteSearch_WithOnlyManagePermission_Returns403()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [EmployeeApiTestData.ManagePermission] };

        var response = await _fixture.PostEmployeeSearchAsync(EmployeeApiTestData.InviteSearchPath, claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Me_WithUserWithoutEmployee_Returns404()
    {
        var claims = _fixture.CreateSupervisorClaims() with { Permissions = [] };

        var response = await _fixture.GetEmployeeAsync(EmployeeApiTestData.MePath, claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
