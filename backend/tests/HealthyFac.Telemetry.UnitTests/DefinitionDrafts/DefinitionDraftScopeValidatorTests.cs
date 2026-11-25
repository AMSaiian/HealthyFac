using HealthyFac.Telemetry.Application.DefinitionDrafts;
using HealthyFac.Telemetry.Application.DefinitionDrafts.SearchDefinitionDrafts;
using HealthyFac.Telemetry.UnitTests.DefinitionDrafts.Data;

namespace HealthyFac.Telemetry.UnitTests.DefinitionDrafts;

public sealed class DefinitionDraftScopeValidatorTests
{
    private static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000c01");

    [Theory]
    [MemberData(nameof(DefinitionDraftScopeTestData.Scopes), MemberType = typeof(DefinitionDraftScopeTestData))]
    public void Search_WithScope_NeedsExactlyOneOfMachineAttemptAndChat(
        string caseName,
        Guid? machineId,
        Guid? attemptId,
        Guid? chatId,
        bool isValid
    )
    {
        var request = new SearchDefinitionDraftsRequest
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            AttemptId = attemptId,
            ChatId = chatId,
            Limit = 25,
            Filter = new DefinitionDraftGridFilter(),
        };

        var result = new SearchDefinitionDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, caseName);
    }

    [Theory]
    [MemberData(nameof(DefinitionDraftScopeTestData.Scopes), MemberType = typeof(DefinitionDraftScopeTestData))]
    public void Dismiss_WithScope_NeedsExactlyOneOfMachineAttemptAndChat(
        string caseName,
        Guid? machineId,
        Guid? attemptId,
        Guid? chatId,
        bool isValid
    )
    {
        var request = new DismissDefinitionDraftsRequest
        {
            CompanyId = CompanyId,
            MachineId = machineId,
            AttemptId = attemptId,
            ChatId = chatId,
        };

        var result = new DismissDefinitionDraftsRequestValidator().Validate(request);

        result.IsValid.Should().Be(isValid, caseName);
    }
}
