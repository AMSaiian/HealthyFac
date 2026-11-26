using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.ApiTests.Shifts;

[Collection(CoreApiCollection.Name)]
[Feature(TestFeatures.Shifts)]
public sealed class ShiftInputTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(ShiftApiTestData.VersionBlockBoundaries), MemberType = typeof(ShiftApiTestData))]
    public async Task CreateVersion_WithBlocksAtTheBoundary_ReturnsExpectedStatus(
        string caseName,
        IReadOnlyList<string> blockRanges,
        HttpStatusCode expectedStatus
    )
    {
        var supervisor = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(supervisor.IdentityUserId, ShiftApiTestData.ManagePermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            ShiftApiTestData.TemplatesPath(supervisor.EncryptedId),
            claims,
            _fixture.CreateVersionBody(blockRanges, ShiftApiTestData.EffectiveFromDaysAhead));

        response.StatusCode.Should().Be(expectedStatus, caseName);
    }

    [Theory]
    [MemberData(nameof(ShiftApiTestData.InvalidDayRanges), MemberType = typeof(ShiftApiTestData))]
    public async Task ScheduleDays_WithInvalidRange_Returns400(string caseName, string from, string to)
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            ShiftApiTestData.ScheduleDaysPath(worker.EncryptedId, from, to),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
    }

    [Fact]
    public async Task FreeSpans_WithMalformedDate_Returns400()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var claims = _fixture.CreateShiftClaims(worker.IdentityUserId, ShiftApiTestData.ReadPermission);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            ShiftApiTestData.FreeSpansPath(worker.EncryptedId, ShiftApiTestData.MalformedDate),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
