using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Application.ErrorHandling;

namespace HealthyFac.Insights.UnitTests.Extraction;

public sealed class ExtractionBufferTests
{
    private static readonly Guid FileMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    [Fact]
    public void ResolveMetric_WithAKeyProposedInTheRun_ReturnsTheDraftId()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        var metric = ExtractionBufferTestData.Metric(key: "motor.temperature");
        buffer.AddMetric(machineId, metric);

        var resolved = buffer.ResolveMetric(machineId, key: "MOTOR.TEMPERATURE");

        resolved.DraftId.Should().Be(metric.DraftId);
        resolved.DefinitionId.Should().BeNull();
    }

    [Fact]
    public void ResolveMetric_WithAKeyOfTheMachineOfTheFile_ReturnsTheDefinitionId()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        var machineId = buffer.AddMachine(name: null, serialNumber: null, description: null);

        var resolved = buffer.ResolveMetric(machineId, key: "motor.temperature");

        resolved.DefinitionId.Should().Be(ExtractionTestData.ExistingMetricId);
        resolved.DraftId.Should().BeNull();
    }

    [Fact]
    public void ResolveStatus_WithACodeOfTheMachineOfTheFile_ReturnsTheDefinitionId()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        var machineId = buffer.AddMachine(name: null, serialNumber: null, description: null);

        var resolved = buffer.ResolveStatus(machineId, code: "E12");

        resolved.DefinitionId.Should().Be(ExtractionTestData.ExistingStatusId);
    }

    [Theory]
    [MemberData(nameof(ExtractionBufferTestData.UnknownKeys), MemberType = typeof(ExtractionBufferTestData))]
    public void Resolve_WithAKeyThatNothingNames_RejectsTheCall(
        string caseName,
        bool isMachineFile,
        Func<ExtractionBuffer, Guid, ResolvedSignal> resolve
    )
    {
        var buffer = CreateBuffer(isMachineFile);
        var machineId = buffer.AddMachine(name: null, serialNumber: null, description: null);

        var act = () => resolve(buffer, machineId);

        act.Should().Throw<UnprocessableException>(caseName);
    }

    [Fact]
    public void AddMachine_WhenAMachineFileHasItsMachine_RejectsTheSecond()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        buffer.AddMachine(name: "Pump", serialNumber: null, description: null);

        var act = () => buffer.AddMachine(name: "Other", serialNumber: null, description: null);

        act.Should().Throw<UnprocessableException>().WithMessage(ExtractionErrors.MachineFileHasOneMachine);
    }

    [Fact]
    public void AddMachine_AtTheMachineLimit_RejectsTheNext()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        for (var index = 0; index < DraftMessageLimits.MaxMachines; index++)
        {
            buffer.AddMachine($"Machine {index}", serialNumber: null, description: null);
        }

        var act = () => buffer.AddMachine(name: "One more", serialNumber: null, description: null);

        act.Should().Throw<UnprocessableException>().WithMessage(ExtractionErrors.TooManyMachines);
    }

    [Fact]
    public void AddMetric_AtTheMetricLimit_RejectsTheNext()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        for (var index = 0; index < DraftMessageLimits.MaxMetrics; index++)
        {
            buffer.AddMetric(machineId, ExtractionBufferTestData.Metric($"metric.m{index}"));
        }

        var act = () => buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "metric.one_more"));

        act.Should().Throw<UnprocessableException>().WithMessage(ExtractionErrors.TooManyMetrics);
    }

    [Fact]
    public void AddMetric_WithARepeatedKey_RejectsTheCall()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "motor.temperature"));

        var act = () => buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "Motor.Temperature"));

        act.Should().Throw<UnprocessableException>();
    }

    [Fact]
    public void AddMetric_ForAMachineThatThisRunDidNotCreate_RejectsTheCall()
    {
        var buffer = CreateBuffer(isMachineFile: false);

        var act = () => buffer.AddMetric(Guid.NewGuid(), ExtractionBufferTestData.Metric(key: "motor.temperature"));

        act.Should().Throw<UnprocessableException>().WithMessage(ExtractionErrors.UnknownMachine().Message);
    }

    [Fact]
    public void FindUnit_WithADiscreteUnit_FindsNothing()
    {
        var buffer = CreateBuffer(isMachineFile: false);

        var unit = buffer.FindUnit(quantityKind: "Discrete", unit: "count");

        unit.Should().BeNull();
    }

    [Fact]
    public void Machines_OfAMachineFile_CarryTheMachineOfTheFileAsTarget()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        buffer.AddMachine(name: "Pump", serialNumber: null, description: null);

        var machines = buffer.Machines;

        machines.Should().ContainSingle().Which.TargetMachineId.Should().Be(FileMachineId);
    }

    private static ExtractionBuffer CreateBuffer(bool isMachineFile) =>
        new(
            isMachineFile,
            isMachineFile ? FileMachineId : null,
            ExtractionTestData.Units,
            ExtractionTestData.Signals);
}
