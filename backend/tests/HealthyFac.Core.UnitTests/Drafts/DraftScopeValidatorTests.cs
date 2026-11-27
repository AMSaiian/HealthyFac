using HealthyFac.Core.Application.Drafts;
using HealthyFac.Core.Application.Drafts.SearchMachineDrafts;
using HealthyFac.Core.UnitTests.Drafts.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.UnitTests.Drafts;

[Feature(TestFeatures.Drafts)]
public sealed class DraftScopeValidatorTests
{
    private static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c01");

    [Theory]
    [MemberData(nameof(DraftScopeTestData.Scopes), MemberType = typeof(DraftScopeTestData))]
    public void Search_WithScope_NeedsExactlyOneOfMachineAttemptAndChat(
        string caseName,
        Guid? machineId,
        Guid? attemptId,
        Guid? chatId,
        bool isValid
    )
    {
        var request = new SearchMachineDraftsRequest
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            AttemptId = attemptId,
            ChatId = chatId,
            Limit = 25,
            Filter = new MachineDraftGridFilter(),
        };

        var result = new SearchMachineDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, caseName);
    }

    [Theory]
    [MemberData(nameof(DraftScopeTestData.Scopes), MemberType = typeof(DraftScopeTestData))]
    public void Dismiss_WithScope_NeedsExactlyOneOfMachineAttemptAndChat(
        string caseName,
        Guid? machineId,
        Guid? attemptId,
        Guid? chatId,
        bool isValid
    )
    {
        var request = new DismissMachineDraftsRequest
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            AttemptId = attemptId,
            ChatId = chatId,
        };

        var result = new DismissMachineDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, caseName);
    }

    [Theory]
    [MemberData(nameof(DraftScopeTestData.GetScopes), MemberType = typeof(DraftScopeTestData))]
    public void GetGroups_WithScope_NeedsExactlyOneOfAttemptAndChat(
        string caseName,
        Guid? attemptId,
        Guid? chatId,
        bool isValid
    )
    {
        var request = new GetAttemptDraftsRequest { CompanyId = CompanyId, AttemptId = attemptId, ChatId = chatId };

        var result = new GetAttemptDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, caseName);
    }
}
