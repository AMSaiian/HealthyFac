using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.Extraction.Signals;
using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

public static class ExtractionTestData
{
    public const string EmptyAnswer = "Nothing to propose.";

    public static readonly Guid CompanyId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000001");

    public static readonly Guid ExistingMetricId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000301");

    public static readonly Guid ExistingStatusId = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000302");

    public static readonly IReadOnlyList<MetricUnit> Units =
    [
        new() { QuantityKind = "temperature", Name = "celsius", Abbreviation = "C" },
        new() { QuantityKind = "pressure", Name = "bar", Abbreviation = "bar" },
        new() { QuantityKind = "Discrete", Name = "count", Abbreviation = "#" },
    ];

    public static readonly MachineSignals Signals = new()
    {
        Metrics = [new SignalReference { Id = ExistingMetricId, Key = "motor.temperature" }],
        Statuses =
        [
            new SignalReference { Id = ExistingStatusId, Key = "E12" },
            new SignalReference { Id = Guid.Parse(input: "0190f3a0-0000-7000-8000-000000000303"), Key = "E13" },
        ],
    };

    public static ExtractionInput CreateInput(bool isMachineFile = false, params ConvertedChunk[] chunks) =>
        new()
        {
            CompanyId = CompanyId,
            Chunks = chunks.Length > 0 ? chunks : [new ConvertedChunk { Location = "page 1", Text = "Pump manual." }],
            Units = Units,
            Signals = Signals,
            IsMachineFile = isMachineFile,
        };
}
