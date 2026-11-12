using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;

namespace HealthyFac.Core.ApiTests.Shifts;

[Collection(CoreApiCollection.Name)]
public sealed class ShiftAuthorizationTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Reads_WithWorkerTokenForOwnEmployee_Return200()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var templates = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.TemplatesPath(worker.EncryptedId), claims);
        var overrides = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.OverridesPath(worker.EncryptedId), claims);
        var schedule = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.SchedulePath(worker.EncryptedId), claims);

        templates.StatusCode.Should().Be(HttpStatusCode.OK);
        overrides.StatusCode.Should().Be(HttpStatusCode.OK);
        schedule.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reads_WithWorkerTokenForOtherEmployee_Return404()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var other = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.OtherEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var templates = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.TemplatesPath(other.EncryptedId), claims);
        var overrides = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.OverridesPath(other.EncryptedId), claims);
        var schedule = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.SchedulePath(other.EncryptedId), claims);

        templates.StatusCode.Should().Be(HttpStatusCode.NotFound);
        overrides.StatusCode.Should().Be(HttpStatusCode.NotFound);
        schedule.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Reads_WithSupervisorTokenForAnyEmployee_Return200()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.OtherEmail);
        var claims = _fixture.CreateShiftClaims(
            supervisor.IdentityUserId,
            ShiftApiTestData.ReadPermission,
            ShiftApiTestData.ManagePermission);

        var schedule = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.SchedulePath(worker.EncryptedId), claims);

        schedule.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(ShiftApiTestData.WorkerWriteRoutes), MemberType = typeof(ShiftApiTestData))]
    public async Task Writes_WithWorkerToken_Return403(string caseName, string method, string pathTemplate)
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var response = await _fixture.SendShiftRequestAsync(
            new HttpMethod(method),
            pathTemplate.Replace("{employeeId}", worker.EncryptedId),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: caseName);
    }

    [Fact]
    public async Task CompanySchedule_WithManageAndEmployeesReadPermissions_Returns200()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(
            supervisor.IdentityUserId,
            ShiftApiTestData.ManagePermission,
            ShiftApiTestData.EmployeesReadPermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, $"{ShiftApiTestData.CompanySchedulePath}?date={ShiftApiTestData.Date}", claims);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CompanySchedule_WithoutEmployeesReadPermission_Returns403()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, $"{ShiftApiTestData.CompanySchedulePath}?date={ShiftApiTestData.Date}", claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Schedule_WithMalformedDate_Returns400()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            ShiftApiTestData.ScheduleWithDatePath(worker.EncryptedId, ShiftApiTestData.MalformedDate),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Schedule_WithMalformedEmployeeId_Returns404()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get, ShiftApiTestData.SchedulePath(ShiftApiTestData.MalformedId), claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
