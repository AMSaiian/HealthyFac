using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.UnitTests.Drafts.Data;

namespace HealthyFac.Core.UnitTests.Drafts;

public sealed class DraftScopeValidatorTests
{
    private static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c01");

    [Theory]
    [MemberData(nameof(DraftScopeTestData.Scopes), MemberType = typeof(DraftScopeTestData))]
    public void Search_WithScope_NeedsExactlyOneOfMachineAndAttempt(
        string caseName,
        Guid? machineId,
        Guid? attemptId,
        bool isValid
    )
    {
        var request = new SearchMachineDraftsRequest
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            AttemptId = attemptId,
            Offset = 0,
            Limit = 25,
            Filter = new MachineDraftGridFilter(),
        };

        var result = new SearchMachineDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, because: caseName);
    }

    [Theory]
    [MemberData(nameof(DraftScopeTestData.Scopes), MemberType = typeof(DraftScopeTestData))]
    public void Dismiss_WithScope_NeedsExactlyOneOfMachineAndAttempt(
        string caseName,
        Guid? machineId,
        Guid? attemptId,
        bool isValid
    )
    {
        var request = new DismissMachineDraftsRequest
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            AttemptId = attemptId,
        };

        var result = new DismissMachineDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, because: caseName);
    }
}
