using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.IntegrationTests.DefinitionDrafts;

[Collection(TelemetryDefinitionDraftsCollection.Name)]
[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class PhaseDraftConstraintTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(PhaseDraftConstraintTestData.InvalidRows), MemberType = typeof(PhaseDraftConstraintTestData))]
    public async Task Insert_OfARowThatBreaksATargetRule_IsRejectedByTheCheckConstraint(
        string caseName,
        string sql,
        string constraintName
    )
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        var act = () => _fixture.ExecuteSqlAsync(sql, ids.PhaseId);

        await act.Should()
            .ThrowAsync<Exception>(caseName)
            .Where(exception => exception.ToString().Contains(constraintName));
    }

    [Theory]
    [MemberData(nameof(PhaseDraftConstraintTestData.ValidRows), MemberType = typeof(PhaseDraftConstraintTestData))]
    public async Task Insert_OfARowWithExactlyOneTarget_IsStored(string caseName, string sql)
    {
        var ids = FlowIds.Create();
        await ProposalEntryFlowTestData.Proposed(_fixture, ids);

        var act = () => _fixture.ExecuteSqlAsync(sql, ids.PhaseId);

        await act.Should().NotThrowAsync(caseName);
    }
}
