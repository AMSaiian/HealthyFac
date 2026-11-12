using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.MetricUnits;
using HealthyFac.Insights.Messaging.Drafts;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

public static class DraftProposalMapperTestData
{
    public const string ValidKey = "motor.temperature";
    public const string ValidName = "Motor temperature";
    public const string Celsius = "celsius";
    public const string Temperature = "temperature";

    public static readonly Guid AttemptId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000301");
    public static readonly Guid FileId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000201");
    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");
    public static readonly Guid MachineId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000101");

    public static readonly DateTime QueuedAt = new(
        year: 2026, month: 11, day: 9, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static IReadOnlyList<MetricUnit> Units { get; } =
    [
        new() { QuantityKind = Temperature, Name = Celsius, Abbreviation = "C" },
        new() { QuantityKind = "pressure", Name = "bar", Abbreviation = "bar" },
        new() { QuantityKind = MetricUnit.DiscreteKind, Name = "count", Abbreviation = "#" },
    ];

    public static TheoryData<string, ExtractedValueKind, string[]> StatesOutsideTheRule => new()
    {
        { "NumericMetric", ExtractedValueKind.Numeric, ["on", "off"] },
        { "FewerThanTwoUniqueStates", ExtractedValueKind.StateSet, ["on", "ON", " "] },
    };

    public static MappingContext Context(bool isMachineFile = false) =>
        new()
        {
            AttemptId = AttemptId,
            FileId = FileId,
            CompanyId = CompanyId,
            IsMachineFile = isMachineFile,
            FileMachineId = isMachineFile ? MachineId : null,
            QueuedAt = QueuedAt,
            Units = Units,
        };

    public static ExtractionResult Result(params ExtractedMachine[] machines) => new() { Machines = machines };

    public static ExtractedMachine Machine(
        IReadOnlyList<ExtractedMetric>? metrics = null,
        IReadOnlyList<ExtractedStatus>? statuses = null,
        string? name = "AX-200 Pump",
        string? serialNumber = null,
        string? description = null
    ) =>
        new()
        {
            Name = name,
            SerialNumber = serialNumber,
            Description = description,
            Metrics = metrics ?? [],
            Statuses = statuses ?? [],
        };

    public static ExtractedMetric Metric(
        string? key = ValidKey,
        string? name = ValidName,
        ExtractedValueKind? valueKind = ExtractedValueKind.Numeric,
        string? quantityKind = null,
        string? unit = null,
        double? min = null,
        double? max = null,
        IReadOnlyList<string>? states = null,
        string? description = null,
        string? sourceLocation = null
    ) =>
        new()
        {
            Key = key,
            Name = name,
            ValueKind = valueKind,
            QuantityKind = quantityKind,
            Unit = unit,
            Min = min,
            Max = max,
            States = states,
            Description = description,
            SourceLocation = sourceLocation,
        };

    public static ExtractedStatus Status(
        string? code = "E12",
        ExtractedSeverity? severity = ExtractedSeverity.Error,
        string? description = null,
        string? sourceLocation = null
    ) =>
        new() { Code = code, Severity = severity, Description = description, SourceLocation = sourceLocation };

    public static ExtractionResult ManyMetrics(int count) =>
        Result(
            Machine(
                metrics: Enumerable.Range(start: 0, count)
                    .Select(index => Metric(key: $"signal.s{index}", name: $"Signal {index}"))
                    .ToList()));

    public static ProposedMachine OnlyMachine(MappingResult result) => result.Message!.Machines.Single();

    public static ProposedMetric OnlyMetric(MappingResult result) => OnlyMachine(result).Metrics.Single();

    public static ProposedStatus OnlyStatus(MappingResult result) => OnlyMachine(result).Statuses.Single();
}
