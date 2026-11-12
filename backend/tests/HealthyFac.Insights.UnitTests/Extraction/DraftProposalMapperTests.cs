using System.Text.Json;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Messaging.Drafts;
using HealthyFac.Insights.UnitTests.Extraction.Data;
using HealthyFac.Shared.Infrastructure.RichText;
using static HealthyFac.Insights.UnitTests.Extraction.Data.DraftProposalMapperTestData;

namespace HealthyFac.Insights.UnitTests.Extraction;

public sealed class DraftProposalMapperTests
{
    private const int KeyTooLong = DraftMessageLimits.Metric.KeyMaxLength + 1;
    private const int ManyMetricCount = 134;

    private readonly DraftProposalMapper _mapper = new(new HtmlSanitizerRichTextPolicy());

    [Fact]
    public void Map_WithEmptyResult_ReturnsNoMessage()
    {
        var result = _mapper.Map(Result(), Context());

        result.Message.Should().BeNull();
        result.DraftCount.Should().Be(expected: 0);
        result.Warning.Should().BeNull();
    }

    [Fact]
    public void Map_WithValidMachine_MapsEveryFieldAndCountsTheDrafts()
    {
        var extraction = Result(
            Machine(
                metrics: [Metric(quantityKind: Temperature, unit: Celsius, min: 0, max: 80, sourceLocation: "page 2")],
                statuses: [Status(description: "Overheating.")],
                serialNumber: "SN-1"));

        var result = _mapper.Map(extraction, Context());

        result.DraftCount.Should().Be(expected: 3);
        result.Message!.AttemptId.Should().Be(AttemptId);
        result.Message.FileId.Should().Be(FileId);
        result.Message.CompanyId.Should().Be(CompanyId);
        result.Message.QueuedAt.Should().Be(QueuedAt);
        OnlyMachine(result).SerialNumber.Should().Be("SN-1");

        var metric = OnlyMetric(result);
        metric.ValueKind.Should().Be(DraftMessageLimits.ValueKinds.Numeric);
        metric.QuantityKind.Should().Be(Temperature);
        metric.Unit.Should().Be(Celsius);
        metric.Min.Should().Be(expected: 0);
        metric.Max.Should().Be(expected: 80);
        metric.SourceLocation.Should().Be("page 2");
        OnlyStatus(result).Severity.Should().Be(DraftMessageLimits.Severities.Error);
        OnlyStatus(result).Description.Should().Be("<p>Overheating.</p>");
    }

    [Fact]
    public void Map_Always_GivesEveryDraftItsOwnVersion7Id()
    {
        var extraction = Result(Machine(metrics: [Metric(), Metric(key: "a.b")], statuses: [Status()]));

        var result = _mapper.Map(extraction, Context());

        var ids = new[] { OnlyMachine(result).DraftId }
            .Concat(OnlyMachine(result).Metrics.Select(metric => metric.DraftId))
            .Concat(OnlyMachine(result).Statuses.Select(status => status.DraftId))
            .ToList();
        ids.Should().OnlyHaveUniqueItems();
        ids.Should().OnlyContain(id => id.Version == 7);
    }

    [Fact]
    public void Map_WithMachineFileAndTwoMachines_KeepsTheFirstWithTheFileMachineAsTarget()
    {
        var extraction = Result(Machine(name: "First"), Machine(name: "Second"));

        var result = _mapper.Map(extraction, Context(isMachineFile: true));

        var machine = OnlyMachine(result);
        machine.Name.Should().Be("First");
        machine.TargetMachineId.Should().Be(MachineId);
    }

    [Fact]
    public void Map_WithCompanyFile_LeavesTheTargetNull()
    {
        var result = _mapper.Map(Result(Machine(), Machine(name: "Second")), Context());

        result.Message!.Machines.Should().HaveCount(expected: 2);
        result.Message.Machines.Should().OnlyContain(machine => machine.TargetMachineId == null);
    }

    [Fact]
    public void Map_WithEmptyMachineWithMetricsOnCompanyFile_KeepsItWithNullFields()
    {
        var extraction = Result(Machine(metrics: [Metric()], name: null));

        var result = _mapper.Map(extraction, Context());

        var machine = OnlyMachine(result);
        machine.Name.Should().BeNull();
        machine.SerialNumber.Should().BeNull();
        machine.Description.Should().BeNull();
        machine.Metrics.Should().ContainSingle();
    }

    [Fact]
    public void Map_WithMachineWithoutValuesAndChildren_DropsIt()
    {
        var result = _mapper.Map(Result(Machine(name: null), Machine(name: "Kept")), Context());

        OnlyMachine(result).Name.Should().Be("Kept");
    }

    [Fact]
    public void Map_WithMetricAndStatusWithoutAnyValue_DropsThem()
    {
        var extraction = Result(
            Machine(
                metrics: [Metric(key: null, name: null, valueKind: null), Metric()],
                statuses: [Status(code: null, severity: null), Status()]));

        var result = _mapper.Map(extraction, Context());

        OnlyMachine(result).Metrics.Should().ContainSingle();
        OnlyMachine(result).Statuses.Should().ContainSingle();
    }

    [Fact]
    public void Map_WithKeyThatBreaksThePattern_NullsTheKey()
    {
        var result = _mapper.Map(Result(Machine(metrics: [Metric(key: "Motor Temperature")])), Context());

        OnlyMetric(result).Key.Should().BeNull();
        OnlyMetric(result).Name.Should().Be(ValidName);
    }

    [Fact]
    public void Map_WithKeyOverTheLimit_NullsTheKey()
    {
        var key = "a" + new string(c: 'b', KeyTooLong - 1);

        var result = _mapper.Map(Result(Machine(metrics: [Metric(key: key)])), Context());

        OnlyMetric(result).Key.Should().BeNull();
    }

    [Fact]
    public void Map_WithCodeOverTheLimit_NullsTheCode()
    {
        var code = new string(c: 'E', DraftMessageLimits.Status.CodeMaxLength + 1);

        var result = _mapper.Map(Result(Machine(statuses: [Status(code: code)])), Context());

        OnlyStatus(result).Code.Should().BeNull();
        OnlyStatus(result).Severity.Should().Be(DraftMessageLimits.Severities.Error);
    }

    [Fact]
    public void Map_WithNameAndSerialNumberOverTheirLimits_NullsThem()
    {
        var name = new string(c: 'n', DraftMessageLimits.Machine.NameMaxLength + 1);
        var serial = new string(c: 's', DraftMessageLimits.Machine.SerialNumberMaxLength + 1);

        var result = _mapper.Map(Result(Machine(name: name, serialNumber: serial, metrics: [Metric()])), Context());

        OnlyMachine(result).Name.Should().BeNull();
        OnlyMachine(result).SerialNumber.Should().BeNull();
    }

    [Fact]
    public void Map_WithMinNotBelowMax_NullsBoth()
    {
        var result = _mapper.Map(Result(Machine(metrics: [Metric(min: 50, max: 50)])), Context());

        OnlyMetric(result).Min.Should().BeNull();
        OnlyMetric(result).Max.Should().BeNull();
    }

    [Fact]
    public void Map_WithOnlyOneBound_KeepsIt()
    {
        var result = _mapper.Map(Result(Machine(metrics: [Metric(max: 80)])), Context());

        OnlyMetric(result).Min.Should().BeNull();
        OnlyMetric(result).Max.Should().Be(expected: 80);
    }

    [Fact]
    public void Map_WithRangeOnBooleanMetric_NullsTheRange()
    {
        var metric = Metric(valueKind: ExtractedValueKind.Boolean, min: 0, max: 1);

        var result = _mapper.Map(Result(Machine(metrics: [metric])), Context());

        OnlyMetric(result).Min.Should().BeNull();
        OnlyMetric(result).Max.Should().BeNull();
    }

    [Theory]
    [MemberData(
        nameof(DraftProposalMapperTestData.StatesOutsideTheRule),
        MemberType = typeof(DraftProposalMapperTestData)
    )]
    public void Map_WithStatesOutsideTheRule_NullsTheStates(
        string caseName,
        ExtractedValueKind valueKind,
        string[] states
    )
    {
        var metric = Metric(valueKind: valueKind, states: states);

        var result = _mapper.Map(Result(Machine(metrics: [metric])), Context());

        OnlyMetric(result).States.Should().BeNull(because: caseName);
    }

    [Fact]
    public void Map_WithRepeatedStates_KeepsTheUniqueOnesInOrder()
    {
        var metric = Metric(valueKind: ExtractedValueKind.StateSet, states: ["idle", "run", "IDLE", "stop"]);

        var result = _mapper.Map(Result(Machine(metrics: [metric])), Context());

        OnlyMetric(result).States.Should().Equal("idle", "run", "stop");
    }

    [Fact]
    public void Map_WithUnitPairOutsideTheCatalog_NullsBoth()
    {
        var metric = Metric(quantityKind: Temperature, unit: "kelvin");

        var result = _mapper.Map(Result(Machine(metrics: [metric])), Context());

        OnlyMetric(result).QuantityKind.Should().BeNull();
        OnlyMetric(result).Unit.Should().BeNull();
    }

    [Fact]
    public void Map_WithDiscretePair_NullsBoth()
    {
        var metric = Metric(quantityKind: "Discrete", unit: "count");

        var result = _mapper.Map(Result(Machine(metrics: [metric])), Context());

        OnlyMetric(result).QuantityKind.Should().BeNull();
        OnlyMetric(result).Unit.Should().BeNull();
    }

    [Fact]
    public void Map_WithUnitInOtherCase_WritesTheCatalogSpelling()
    {
        var metric = Metric(quantityKind: "TEMPERATURE", unit: "Celsius");

        var result = _mapper.Map(Result(Machine(metrics: [metric])), Context());

        OnlyMetric(result).QuantityKind.Should().Be(Temperature);
        OnlyMetric(result).Unit.Should().Be(Celsius);
    }

    [Fact]
    public void Map_WithLongSourceLocation_CutsItToTheLimit()
    {
        var location = new string(c: 'p', DraftMessageLimits.SourceLocationMaxLength + 50);

        var result = _mapper.Map(Result(Machine(metrics: [Metric(sourceLocation: location)])), Context());

        OnlyMetric(result).SourceLocation.Should().HaveLength(DraftMessageLimits.SourceLocationMaxLength);
    }

    [Fact]
    public void Map_WithDuplicateKeysAndCodes_KeepsTheFirstOfEach()
    {
        var extraction = Result(
            Machine(
                metrics: [Metric(name: "First"), Metric(name: "Second", key: "MOTOR.TEMPERATURE".ToLowerInvariant())],
                statuses: [Status(description: "First"), Status(description: "Second")]));

        var result = _mapper.Map(extraction, Context());

        OnlyMetric(result).Name.Should().Be("First");
        OnlyStatus(result).Description.Should().Be("<p>First</p>");
    }

    [Fact]
    public void Map_WithMarkupInDescription_EncodesItAndSplitsParagraphs()
    {
        var text = "<script>alert(1)</script>\n\nSecond paragraph.";

        var result = _mapper.Map(Result(Machine(description: text, metrics: [Metric()])), Context());

        OnlyMachine(result).Description.Should()
            .Be("<p>&lt;script&gt;alert(1)&lt;/script&gt;</p><p>Second paragraph.</p>");
    }

    [Fact]
    public void Map_WithDescriptionOverTheVisibleLimit_NullsIt()
    {
        var text = new string(c: 'x', DraftMessageLimits.DescriptionMaxLength + 1);

        var result = _mapper.Map(Result(Machine(description: text, metrics: [Metric()])), Context());

        OnlyMachine(result).Description.Should().BeNull();
    }

    [Fact]
    public void Map_WithMoreMetricsThanTheCap_KeepsTheFirstAndWarns()
    {
        var result = _mapper.Map(ManyMetrics(ManyMetricCount), Context());

        OnlyMachine(result).Metrics.Should().HaveCount(DraftMessageLimits.MaxMetrics);
        OnlyMachine(result).Metrics[index: 0].Key.Should().Be("signal.s0");

        using var warning = JsonDocument.Parse(result.Warning!);
        var values = warning.RootElement.GetProperty(propertyName: "values");
        warning.RootElement.GetProperty(propertyName: "key").GetString().Should().Be("INGEST.WARNINGS.TRUNCATED");
        values.GetProperty(propertyName: "found").GetInt32().Should().Be(ManyMetricCount);
        values.GetProperty(propertyName: "cap").GetInt32().Should().Be(DraftMessageLimits.MaxMetrics);
        values.GetProperty(propertyName: "kind").GetString().Should().Be("metrics");
    }

    [Fact]
    public void Map_WithMachineFileWithoutFileMachine_ThrowsInvalidExtraction()
    {
        var context = Context(isMachineFile: true) with { FileMachineId = null };

        var act = () => _mapper.Map(Result(Machine()), context);

        act.Should().Throw<InvalidExtractionException>();
    }
}
