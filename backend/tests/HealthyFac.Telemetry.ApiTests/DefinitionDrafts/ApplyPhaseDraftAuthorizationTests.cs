using System.Net;
using HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts;

[Collection(TelemetryApiCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class ApplyPhaseDraftAuthorizationTests(TelemetryApiFixture fixture)
{
    private const string Path =
        "v1/machines/not-checked-before-authorization/definition-drafts/operating-phases/x/apply";

    private const string DraftsWrite = "definition-drafts:write";
    private const string PhasesWrite = "operating-phases:write";

    private const string Body =
        "{\"code\":\"cutting\",\"name\":\"Cutting\",\"priority\":0,\"isTracked\":true,\"conditions\":[],\"limits\":[]}";

    private readonly TelemetryApiFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(ApplyPhaseDraftAuthorizationTestData.MissingOnePermission),
        MemberType = typeof(ApplyPhaseDraftAuthorizationTestData))]
    public async Task ApplyPhase_WithOnlyOneOfTheTwoWritePermissions_Returns403(string caseName, string permission)
    {
        var response = await _fixture.SendAsync(
            HttpMethod.Post, Path, _fixture.CreateClaims(permission), Body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Fact]
    public async Task ApplyPhase_WithBothWritePermissions_IsNotForbidden()
    {
        var response = await _fixture.SendAsync(
            HttpMethod.Post, Path, _fixture.CreateClaims(DraftsWrite, PhasesWrite), Body);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }
}
