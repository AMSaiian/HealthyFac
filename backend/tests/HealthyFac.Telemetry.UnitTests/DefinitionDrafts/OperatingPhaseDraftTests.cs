using HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts;

[Feature(TestFeatures.Drafts)]
[Feature(TestFeatures.SignalModel)]
public sealed class OperatingPhaseDraftTests
{
    [Fact]
    public void RetargetStatus_WithTwoConditionsOnTheStatusDraft_MovesBothConditions()
    {
        var draft = OperatingPhaseDraftTestData.CreateWithTwoConditionsOnOneStatusDraft();

        var isMoved = draft.RetargetStatus(
            OperatingPhaseDraftTestData.StatusDraftId, OperatingPhaseDraftTestData.StatusDefinitionId);

        isMoved.Should().BeTrue();

        draft.Conditions.Should()
            .OnlyContain(condition => condition.StatusDraftId == null
                && condition.StatusDefinitionId == OperatingPhaseDraftTestData.StatusDefinitionId);
    }
}
