using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;

namespace HealthyFac.Core.ApiTests.Shifts;

[Collection(CoreApiCollection.Name)]
public sealed class ShiftInputTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task CreateVersion_WithBlockEndingAtMidnight_Returns400()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            ShiftApiTestData.TemplatesPath(supervisor.EncryptedId),
            claims,
            _fixture.CreateVersionBody(ShiftApiTestData.MidnightEndBlock, ShiftApiTestData.EffectiveFromDaysAhead));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateVersion_WithBlockEndingAt2355_Returns201()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            ShiftApiTestData.TemplatesPath(supervisor.EncryptedId),
            claims,
            _fixture.CreateVersionBody(ShiftApiTestData.LatestEndBlock, ShiftApiTestData.EffectiveFromDaysAhead));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task DeleteOverride_WithoutRowVersion_Returns400()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Delete,
            ShiftApiTestData.OverridePath(supervisor.EncryptedId),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteVersion_WithoutRowVersion_Returns400()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);

        var versionId = await _fixture.ArrangeVersionAsync(supervisor, claims);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Delete,
            $"{ShiftApiTestData.TemplatesPath(supervisor.EncryptedId)}/{versionId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteVersion_WithStaleRowVersion_Returns409NotBadRequest()
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);
        var versionId = await _fixture.ArrangeVersionAsync(supervisor, claims);

        var path = $"{ShiftApiTestData.TemplatesPath(supervisor.EncryptedId)}/{versionId}";

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Delete,
            $"{path}?rowVersion={ShiftApiTestData.StaleRowVersion}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
