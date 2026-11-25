using HealthyFac.Core.Messaging.Drafts;
using HealthyFac.Core.UnitTests.Messaging.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Messaging;

[Feature(TestFeatures.Messaging)]
[Feature(TestFeatures.Drafts)]
public sealed class MachineDraftAppliedValidatorTests
{
    [Theory]
    [MemberData(nameof(MachineDraftAppliedTestData.FileAndTime), MemberType = typeof(MachineDraftAppliedTestData))]
    public void Validate_WithFileAndQueueTime_RequiresBothOrNeither(
        string caseName,
        Guid? fileId,
        DateTime? queuedAt,
        bool isValid
    )
    {
        var message = new MachineDraftApplied
        {
            DraftId = MachineDraftAppliedTestData.Id,
            MachineId = MachineDraftAppliedTestData.Id,
            CompanyId = MachineDraftAppliedTestData.Id,
            FileId = fileId,
            QueuedAt = queuedAt,
        };

        var result = new MachineDraftAppliedValidator().Validate(message);

        result.IsValid.Should().Be(isValid, caseName);
    }
}
