using System.Net;
using HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;
using HealthyFac.Telemetry.ApiTests.Hosting;
using HealthyFac.Telemetry.ApiTests.MetricDefinitions.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts;

[Collection(TelemetryApiCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class DefinitionDraftInputTests(TelemetryApiFixture fixture)
{
    private readonly TelemetryApiFixture _fixture = fixture;

    [Fact]
    public async Task Dismiss_OfADraftThatIsNotPending_Returns422WithTheDraftNotPendingKey()
    {
        var draft = await _fixture.ArrangeDismissedMetricDraftAsync();
        var claims = _fixture.CreateClaims(DefinitionDraftAuthorizationTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Post,
            DefinitionDraftInputTestData.DismissPath(draft.DraftId),
            claims,
            DefinitionDraftInputTestData.DismissBody);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        content.Should().Contain(DefinitionDraftInputTestData.DraftNotPendingKey);
    }

    [Fact]
    public async Task ApplyMetrics_WithADraftThatIsNotPending_Returns422WithTheDraftNotPendingKey()
    {
        var draft = await _fixture.ArrangeDismissedMetricDraftAsync();

        var claims = _fixture.CreateClaims(
            MetricDefinitionApiTestData.ReadPermission,
            MetricDefinitionApiTestData.WritePermission);

        var response = await _fixture.SendAsync(
            HttpMethod.Put,
            _fixture.DefinitionsPath(draft.MachineId),
            claims,
            _fixture.ApplyBodyWithDraft(draft.DraftId));

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        content.Should().Contain(DefinitionDraftInputTestData.DraftNotPendingKey);
    }
}
