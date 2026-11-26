using System.Text.Json;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.Messaging.WorkOrders;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.IdEncryption;

namespace HealthyFac.Insights.IntegrationTests.Chats;

[Collection(ChatTurnCollection.Name)]
public sealed class ChatDraftToolTests(ChatTurnFixture fixture)
{
    private const string MetricArguments =
        """{"machineId":"%ID%","key":"temp","name":"Temperature","valueKind":"numeric"}""";

    private const string WorkOrderArguments =
        """{"title":"Replace the filter","priority":"%PRIORITY%","category":"repair","estimateMinutes":30}""";

    private readonly ChatTurnFixture _fixture = fixture;

    [Fact]
    public async Task ProposeMetric_ForAnExistingMachine_MapsToTheMachineIdWithNoMachineDraft()
    {
        using var session = await _fixture.StartToolSessionAsync();

        await session.CallAsync(toolName: "propose_metric_draft", MetricFor(ChatTestData.MachineId));

        var metric = session.Buffer.Metrics.Should().ContainSingle().Subject;
        metric.MachineId.Should().Be(ChatTestData.MachineId);
        metric.MachineDraftId.Should().BeNull();
        session.Buffer.Machines.Should().BeEmpty();
    }

    [Fact]
    public async Task ProposeMetric_ForACreateDraftOfTheTurn_MapsToTheMachineDraftId()
    {
        using var session = await _fixture.StartToolSessionAsync();
        var machineResult = await session.CallAsync(
            toolName: "propose_machine_draft", argumentsJson: """{"name":"Lathe 9"}""");
        var machineId = ReadMachineId(machineResult);

        await session.CallAsync(toolName: "propose_metric_draft", MetricArguments.Replace(oldValue: "%ID%", machineId));

        var machine = session.Buffer.Machines.Should().ContainSingle().Subject;
        machine.TargetMachineId.Should().BeNull();
        var metric = session.Buffer.Metrics.Should().ContainSingle().Subject;
        metric.MachineDraftId.Should().Be(machine.DraftId);
        metric.MachineId.Should().BeNull();
    }

    [Fact]
    public async Task ProposeMetric_ForAMachineThatTheUserMayNotRead_GivesTheModelTheFixedTextAndBuffersNothing()
    {
        _fixture.Fakes.Reads.MachineFailure = new NotFoundException(ChatToolErrors.NotFoundOrNotAllowed);
        using var session = await _fixture.StartToolSessionAsync();

        var result = await session.CallAsync(toolName: "propose_metric_draft", MetricFor(Guid.CreateVersion7()));

        result.Should().Contain(ChatToolErrors.NotFoundOrNotAllowed);
        session.Buffer.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task ProposeMetric_WithAnInvalidValueKind_IsRejectedAndBuffersNothing()
    {
        using var session = await _fixture.StartToolSessionAsync();
        var arguments = MetricFor(ChatTestData.MachineId).Replace(oldValue: "numeric", newValue: "nonsense");

        var result = await session.CallAsync(toolName: "propose_metric_draft", arguments);

        var calls = await _fixture.ReadToolCallsAsync(session.RunId);
        session.Buffer.IsEmpty.Should().BeTrue();
        result.Should().Contain(DraftMessageErrors.Validation.ValueKindInvalid);
        calls.Should().ContainSingle().Which.Outcome.Should().Be(ToolCallOutcome.Rejected);
    }

    [Fact]
    public async Task ProposeMetric_WithARepeatedKeyOnTheSameMachine_BuffersOnlyTheFirst()
    {
        using var session = await _fixture.StartToolSessionAsync();
        await session.CallAsync(toolName: "propose_metric_draft", MetricFor(ChatTestData.MachineId));

        await session.CallAsync(toolName: "propose_metric_draft", MetricFor(ChatTestData.MachineId));

        session.Buffer.Metrics.Should().ContainSingle();
    }

    [Fact]
    public async Task ProposeWorkOrder_WithAnInvalidPriority_IsRejectedAndBuffersNothing()
    {
        using var session = await _fixture.StartToolSessionAsync();

        var result = await session.CallAsync(
            toolName: "propose_work_order_draft",
            WorkOrderArguments.Replace(oldValue: "%PRIORITY%", newValue: "urgent"));

        session.Buffer.IsEmpty.Should().BeTrue();
        result.Should().Contain(WorkOrderDraftMessageErrors.Validation.PriorityInvalid);
    }

    [Fact]
    public async Task ProposeWorkOrder_WithValidValues_BuffersTheWorkOrderAndGivesItsDraftRow()
    {
        using var session = await _fixture.StartToolSessionAsync();

        await session.CallAsync(
            toolName: "propose_work_order_draft", WorkOrderArguments.Replace(oldValue: "%PRIORITY%", newValue: "high"));

        var workOrder = session.Buffer.WorkOrders.Should().ContainSingle().Subject;
        workOrder.Title.Should().Be(expected: "Replace the filter");
        session.Buffer.DraftRows.Should().ContainSingle().Which.DraftId.Should().Be(workOrder.DraftId);
    }

    [Fact]
    public async Task ProposeMachine_ForAnUpdateAfterADefinitionOfTheSameMachine_SetsTheValuesOnTheSameEntry()
    {
        using var session = await _fixture.StartToolSessionAsync();
        await session.CallAsync(toolName: "propose_metric_draft", MetricFor(ChatTestData.MachineId));

        await session.CallAsync(
            toolName: "propose_machine_draft", UpdateFor(ChatTestData.MachineId, name: "Lathe 7 B"));

        var machine = session.Buffer.Machines.Should().ContainSingle().Subject;
        machine.TargetMachineId.Should().Be(ChatTestData.MachineId);
        machine.Name.Should().Be(expected: "Lathe 7 B");
        session.Buffer.Metrics.Should().ContainSingle();
    }

    [Fact]
    public async Task ProposeMachine_ForAnUpdateBeforeADefinitionOfTheSameMachine_KeepsBothOnTheSameEntry()
    {
        using var session = await _fixture.StartToolSessionAsync();
        await session.CallAsync(
            toolName: "propose_machine_draft", UpdateFor(ChatTestData.MachineId, name: "Lathe 7 B"));

        await session.CallAsync(toolName: "propose_metric_draft", MetricFor(ChatTestData.MachineId));

        session.Buffer.Machines.Should().ContainSingle().Which.Name.Should().Be(expected: "Lathe 7 B");
        session.Buffer.Metrics.Should().ContainSingle().Which.MachineId.Should().Be(ChatTestData.MachineId);
    }

    [Fact]
    public async Task ProposeMachine_ForASecondUpdateOfTheSameMachine_IsRejectedAndKeepsTheFirstValues()
    {
        using var session = await _fixture.StartToolSessionAsync();
        await session.CallAsync(toolName: "propose_machine_draft", UpdateFor(ChatTestData.MachineId, name: "First"));

        var result = await session.CallAsync(
            toolName: "propose_machine_draft", UpdateFor(ChatTestData.MachineId, name: "Second"));

        result.Should().Contain(ExtractionErrors.MachineValuesSetOnce);
        session.Buffer.Machines.Should().ContainSingle().Which.Name.Should().Be(expected: "First");
    }

    private string UpdateFor(Guid machineId, string name) =>
        $$"""{"targetMachineId":"{{EncryptedId(machineId)}}","name":"{{name}}"}""";

    private string EncryptedId(Guid id) => _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(id);

    private string MetricFor(Guid machineId) =>
        MetricArguments.Replace(oldValue: "%ID%", _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(machineId));

    private static string ReadMachineId(string result) =>
        JsonDocument.Parse(result[result.IndexOf(value: '{')..(result.LastIndexOf(value: '}') + 1)])
            .RootElement
            .GetProperty(propertyName: "machineId")
            .GetString()!;
}
