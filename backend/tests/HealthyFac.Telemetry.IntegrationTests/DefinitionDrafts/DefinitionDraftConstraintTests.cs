using HealthyFac.Telemetry.Domain.DefinitionDrafts;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
[Feature(TestFeatures.Persistence)]
public sealed class DefinitionDraftConstraintTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(DefinitionDraftTestData.InvalidUpdates), MemberType = typeof(DefinitionDraftTestData))]
    public async Task Update_ThatBreaksAConstraint_IsRejectedByTheCheckConstraint(
        string caseName,
        InvalidUpdateCase invalidUpdate
    )
    {
        var rows = await _fixture.StoreDraftRowsAsync();

        var act = () => _fixture.ExecuteSqlAsync(invalidUpdate.Sql, invalidUpdate.RowId(rows));

        await act.Should()
            .ThrowAsync<Exception>(caseName)
            .Where(exception => exception.ToString().Contains(invalidUpdate.ConstraintName));
    }

    [Fact]
    public async Task Insert_OfAwaitingDraftWithoutTarget_IsStored()
    {
        var rows = await _fixture.StoreDraftRowsAsync();

        var draft = await _fixture.LoadMetricDraftAsync(rows.MetricId);

        draft.State.Should().Be(DefinitionDraftState.AwaitingTarget);
        draft.TargetMachineId.Should().BeNull();
        draft.TargetMachineDraftId.Should().BeNull();
    }
}
