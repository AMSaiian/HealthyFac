using System.Text.Json;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Domain.Agents.ToolCalls;
using HealthyFac.Insights.IntegrationTests.Chats.Data;
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

        await session.CallAsync(toolName: "propose_metric_draft", arguments);

        var calls = await _fixture.ReadToolCallsAsync(session.RunId);
        session.Buffer.IsEmpty.Should().BeTrue();
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

        await session.CallAsync(
            toolName: "propose_work_order_draft",
            WorkOrderArguments.Replace(oldValue: "%PRIORITY%", newValue: "urgent"));

        session.Buffer.IsEmpty.Should().BeTrue();
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

    private string MetricFor(Guid machineId) =>
        MetricArguments.Replace(oldValue: "%ID%", _fixture.Inner.GetSingleton<IIdEncryptor>().Encrypt(machineId));

    private static string ReadMachineId(string result) =>
        JsonDocument.Parse(result[result.IndexOf(value: '{')..(result.LastIndexOf(value: '}') + 1)])
            .RootElement
            .GetProperty(propertyName: "machineId")
            .GetString()!;
}
