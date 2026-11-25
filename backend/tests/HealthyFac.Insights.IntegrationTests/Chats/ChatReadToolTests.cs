using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(ChatTurnCollection.Name)]
public sealed class ChatReadToolTests(ChatTurnFixture fixture)
{
    private const string ForeignGuidText = "11111111-1111-1111-1111-111111111111";

    private readonly ChatTurnFixture _fixture = fixture;

    [Theory]
    [InlineData("search_machines", """{"name":"Pre"}""")]
    [InlineData("get_work_order", """{"workOrderId":"%ID%"}""")]
    public async Task ChatTool_WhenTheReadIsNotFoundOrNotAllowed_GivesTheModelTheFixedTextAndLogsNoReplyBody(
        string toolName,
        string argumentsTemplate
    )
    {
        _fixture.Fakes.Reads.Failure = new NotFoundException(ChatToolErrors.NotFoundOrNotAllowed);
        var encryptedId = _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(Guid.CreateVersion7());

        var (result, runId) = await _fixture.CallChatToolAsync(
            toolName,
            argumentsTemplate.Replace(oldValue: "%ID%", encryptedId));

        result.Should().Contain(ChatToolErrors.NotFoundOrNotAllowed);
        var call = (await _fixture.ReadToolCallsAsync(runId)).Should().ContainSingle().Subject;
        call.Outcome.Should().Be(ToolCallOutcome.Rejected);
        call.Result.Should().Be($"\"{ChatToolErrors.NotFoundOrNotAllowed}\"");
    }

    [Fact]
    public async Task SearchMachines_WithArgumentsOfTheModel_PassesThemUnchangedAndOwnsNoCompanyOrActorField()
    {
        _fixture.Fakes.Reads.MachinesAnswer = new SearchMachinesResponse { TotalCount = 0, Machines = [] };

        await _fixture.CallChatToolAsync(
            toolName: "search_machines",
            $$"""{"name":"Pre","limit":5,"companyId":"{{ForeignGuidText}}","actorId":"{{ForeignGuidText}}"}""");

        var arguments = _fixture.Fakes.Reads.Calls.Should()
            .ContainSingle()
            .Subject
            .Should()
            .BeOfType<SearchMachinesArguments>()
            .Subject;
        arguments.Name.Should().Be(expected: "Pre");
        arguments.Limit.Should().Be(expected: 5);
        typeof(SearchMachinesArguments).GetProperties()
            .Select(property => property.Name)
            .Should()
            .NotContain(["CompanyId", "ActorId", "UserId"]);
    }

    [Fact]
    public async Task SearchMachines_WithAMachineAnswer_ReturnsEncryptedIdsOnly()
    {
        var machineId = Guid.CreateVersion7();
        _fixture.Fakes.Reads.MachinesAnswer = new SearchMachinesResponse
        {
            TotalCount = 1,
            Machines =
            [
                new ChatMachine
                {
                    Id = machineId,
                    Name = "Press",
                    SerialNumber = "SN-1",
                    State = "operational",
                    ExpectedGoLiveOn = "2026-01-01",
                },
            ],
        };

        var (result, runId) = await _fixture.CallChatToolAsync(toolName: "search_machines", argumentsJson: "{}");

        var encryptedId = _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(machineId);
        result.Should().Contain(encryptedId).And.NotContain(machineId.ToString());
        var call = (await _fixture.ReadToolCallsAsync(runId)).Should().ContainSingle().Subject;
        call.Result.Should().Contain(encryptedId).And.NotContain(machineId.ToString());
        call.Outcome.Should().Be(ToolCallOutcome.Succeeded);
    }

    [Fact]
    public async Task GetCase_ForACaseThatDoesNotExistInTheCompany_IsRejectedToTheModel()
    {
        var encryptedId = _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(Guid.CreateVersion7());

        var (_, runId) = await _fixture.CallChatToolAsync(toolName: "get_case", $$"""{"caseId":"{{encryptedId}}"}""");

        var call = (await _fixture.ReadToolCallsAsync(runId)).Should().ContainSingle().Subject;
        call.Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task SearchCases_WithAnUnknownState_IsRejectedWithTheValidationText()
    {
        var (result, runId) = await _fixture.CallChatToolAsync(
            toolName: "search_cases", argumentsJson: """{"states":["archived"]}""");

        result.Should().Contain(ChatToolErrors.CaseStateUnknown);
        var call = (await _fixture.ReadToolCallsAsync(runId)).Should().ContainSingle().Subject;
        call.Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task SearchCases_ForACompanyWithoutCases_ReturnsAnEmptyPage()
    {
        var (result, runId) = await _fixture.CallChatToolAsync(toolName: "search_cases", argumentsJson: "{}");

        result.Should().Contain(expected: "\"totalCount\": 0");
        (await _fixture.ReadToolCallsAsync(runId)).Single().Outcome.Should().Be(ToolCallOutcome.Succeeded);
    }
}
