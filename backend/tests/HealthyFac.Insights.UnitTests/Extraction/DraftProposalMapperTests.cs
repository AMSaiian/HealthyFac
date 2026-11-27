using HealthyFac.Insights.Application.Extraction.Mapping;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Infrastructure.RichText;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.Extraction;

[Feature(TestFeatures.Ingest)]
public sealed class DraftProposalMapperTests
{
    private static readonly Guid FileMachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000a01");

    private readonly DraftProposalMapper _mapper = new(new HtmlSanitizerRichTextPolicy());

    [Fact]
    public void Map_WithAMachineThatHasNoContent_ReturnsNoMessage()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        buffer.AddMachine(name: null, serialNumber: null, description: null);

        var result = _mapper.Map(buffer, CreateContext(isMachineFile: false));

        result.Message.Should().BeNull();
        result.DraftCount.Should().Be(expected: 0);
    }

    [Fact]
    public void Map_ForACompanyFileWithMetricsStatusesAndPhases_SendsOneCreateDraftAndTheDefinitionsWithItsDraftId()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "motor.temperature"));
        buffer.AddStatus(machineId, new ProposedStatus { DraftId = Guid.CreateVersion7(), Code = "E12" });
        buffer.AddPhase(machineId, new ProposedPhase { DraftId = Guid.CreateVersion7(), Code = "idle" });

        var result = _mapper.Map(buffer, CreateContext(isMachineFile: false));

        var message = result.Message!;
        var machine = message.Machines.Should().ContainSingle().Subject;
        result.DraftCount.Should().Be(expected: 4);
        message.Source.Should().Be(ProposalSource.Ingest);
        machine.TargetMachineId.Should().BeNull();
        message.Metrics.Should().ContainSingle().Which.MachineDraftId.Should().Be(machine.DraftId);
        message.Statuses.Should().ContainSingle().Which.MachineDraftId.Should().Be(machine.DraftId);
        message.Phases.Should().ContainSingle().Which.MachineDraftId.Should().Be(machine.DraftId);
    }

    [Fact]
    public void Map_ForACompanyFileWithOnlyAMetric_SendsTheCreateDraftOfItsMachine()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        var machineId = buffer.AddMachine(name: null, serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "motor.temperature"));

        var result = _mapper.Map(buffer, CreateContext(isMachineFile: false));

        result.Message!.Machines.Should().ContainSingle().Which.DraftId.Should().Be(machineId);
    }

    [Fact]
    public void Map_ForAMachineFileWithOnlyMetrics_SendsNoMachineAndTheMetricsWithTheMachineOfTheFile()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        var machineId = buffer.AddMachine(name: null, serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "motor.temperature"));

        var result = _mapper.Map(buffer, CreateContext(isMachineFile: true));

        var message = result.Message!;
        message.Machines.Should().BeEmpty();
        message.Metrics.Should().ContainSingle().Which.MachineId.Should().Be(FileMachineId);
        message.Metrics[0].MachineDraftId.Should().BeNull();
    }

    [Fact]
    public void Map_ForAMachineFileWithANameChange_SendsOneUpdateDraftAndTheMetricsWithTheMachineOfTheFile()
    {
        var buffer = CreateBuffer(isMachineFile: true);
        var machineId = buffer.AddMachine(name: "Renamed pump", serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "motor.temperature"));

        var result = _mapper.Map(buffer, CreateContext(isMachineFile: true));

        var message = result.Message!;
        message.Machines.Should().ContainSingle().Which.TargetMachineId.Should().Be(FileMachineId);
        message.Metrics.Should().ContainSingle().Which.MachineId.Should().Be(FileMachineId);
    }

    [Fact]
    public void Map_ForACompanyFileWithTwoMachinesOf60MetricsEach_PassesTheMessageRules()
    {
        var buffer = CreateBuffer(isMachineFile: false);

        foreach (var machineName in new[] { "Pump", "Press" })
        {
            var machineId = buffer.AddMachine(machineName, serialNumber: null, description: null);

            for (var index = 0; index < 60; index++)
            {
                buffer.AddMetric(machineId, ExtractionBufferTestData.Metric($"motor.temperature_{index}"));
            }
        }

        var result = _mapper.Map(buffer, CreateContext(isMachineFile: false));

        result.Message!.Metrics.Should().HaveCount(expected: 120);
    }

    [Fact]
    public void Map_WithAMetricThatBreaksTheMessageRules_FailsTheExtraction()
    {
        var buffer = CreateBuffer(isMachineFile: false);
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "Bad Key"));

        var act = () => _mapper.Map(buffer, CreateContext(isMachineFile: false));

        act.Should().Throw<HealthyFac.Insights.Application.Extraction.InvalidExtractionException>();
    }

    private static ExtractionBuffer CreateBuffer(bool isMachineFile) =>
        new(
            isMachineFile,
            isMachineFile ? FileMachineId : null,
            ExtractionTestData.Units,
            ExtractionTestData.Signals);

    private static MappingContext CreateContext(bool isMachineFile) =>
        new()
        {
            AttemptId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            CompanyId = ExtractionTestData.CompanyId,
            IsMachineFile = isMachineFile,
            FileMachineId = isMachineFile ? FileMachineId : null,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 18, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
        };
}
