using System.Net;
using HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts;

[Collection(TelemetryApiCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class DefinitionDraftAuthorizationTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Theory]
    [MemberData(
        nameof(DefinitionDraftAuthorizationTestData.Routes),
        MemberType = typeof(DefinitionDraftAuthorizationTestData))]
    public async Task Route_WithoutItsPermission_Returns403(
        string caseName,
        string method,
        string path,
        string? body,
        string permission
    )
    {
        var otherPermission = permission == DefinitionDraftAuthorizationTestData.ReadPermission
            ? DefinitionDraftAuthorizationTestData.WritePermission
            : DefinitionDraftAuthorizationTestData.ReadPermission;

        var response = await _fixture.SendAsync(
            new HttpMethod(method), path, _fixture.CreateClaims(otherPermission), body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Theory]
    [MemberData(
        nameof(DefinitionDraftAuthorizationTestData.Routes),
        MemberType = typeof(DefinitionDraftAuthorizationTestData))]
    public async Task Route_WithItsPermission_IsNotForbidden(
        string caseName,
        string method,
        string path,
        string? body,
        string permission
    )
    {
        var response = await _fixture.SendAsync(
            new HttpMethod(method), path, _fixture.CreateClaims(permission), body);

        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden, caseName);
    }
}
