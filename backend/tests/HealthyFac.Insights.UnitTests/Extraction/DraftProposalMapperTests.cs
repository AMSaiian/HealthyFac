using HealthyFac.Insights.Application.Extraction.Mapping;
using HealthyFac.Insights.Application.Extraction.Proposals;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Infrastructure.RichText;

namespace HealthyFac.Insights.UnitTests.Extraction;

public sealed class DraftProposalMapperTests
{
    private readonly DraftProposalMapper _mapper = new(new HtmlSanitizerRichTextPolicy());

    [Fact]
    public void Map_WithAMachineThatHasNoContent_ReturnsNoMessage()
    {
        var buffer = CreateBuffer();
        buffer.AddMachine(name: null, serialNumber: null, description: null);

        var result = _mapper.Map(buffer, CreateContext());

        result.Message.Should().BeNull();
        result.DraftCount.Should().Be(expected: 0);
    }

    [Fact]
    public void Map_WithMetricsStatusesAndPhases_CountsEveryDraft()
    {
        var buffer = CreateBuffer();
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "motor.temperature"));
        buffer.AddStatus(machineId, new ProposedStatus { DraftId = Guid.CreateVersion7(), Code = "E12" });
        buffer.AddPhase(machineId, new ProposedPhase { DraftId = Guid.CreateVersion7(), Code = "idle" });

        var result = _mapper.Map(buffer, CreateContext());

        result.DraftCount.Should().Be(expected: 4);
        result.Message!.Machines.Should().ContainSingle().Which.Phases.Should().ContainSingle();
    }

    [Fact]
    public void Map_WithAMetricThatBreaksTheMessageRules_FailsTheExtraction()
    {
        var buffer = CreateBuffer();
        var machineId = buffer.AddMachine(name: "Pump", serialNumber: null, description: null);
        buffer.AddMetric(machineId, ExtractionBufferTestData.Metric(key: "Bad Key"));

        var act = () => _mapper.Map(buffer, CreateContext());

        act.Should().Throw<HealthyFac.Insights.Application.Extraction.InvalidExtractionException>();
    }

    private static ExtractionBuffer CreateBuffer() =>
        new(isMachineFile: false, fileMachineId: null, ExtractionTestData.Units, ExtractionTestData.Signals);

    private static MappingContext CreateContext() =>
        new()
        {
            AttemptId = Guid.NewGuid(),
            FileId = Guid.NewGuid(),
            CompanyId = ExtractionTestData.CompanyId,
            IsMachineFile = false,
            QueuedAt = new DateTime(year: 2026, month: 11, day: 18, hour: 8, minute: 0, second: 0, DateTimeKind.Utc),
        };
}
