using HealthyFac.Insights.Application.Conversion;
using HealthyFac.Insights.Application.Extraction;
using HealthyFac.Insights.Application.MetricUnits;

namespace HealthyFac.Insights.UnitTests.Extraction.Data;

public static class ExtractionTestData
{
    public const string ValidAnswer = """
        {
          "machines": [
            {
              "name": "AX-200 Rotary Pump",
              "serialNumber": null,
              "description": "A rotary pump.",
              "metrics": [
                {
                  "key": "motor.temperature", "name": "Motor temperature", "description": null,
                  "valueKind": "numeric", "quantityKind": "temperature", "unit": "celsius",
                  "min": 0, "max": 80, "states": null, "sourceLocation": "page 2"
                }
              ],
              "statuses": [
                { "code": "E12", "severity": "error", "description": "Overheating.", "sourceLocation": "page 3" }
              ]
            }
          ]
        }
        """;

    public const string EmptyAnswer = """{ "machines": [] }""";

    public const string UnknownEnumAnswer = """
        { "machines": [ { "metrics": [ { "valueKind": "sideways" } ], "statuses": [] } ] }
        """;

    public const string ExtraPropertyAnswer = """
        { "machines": [], "comment": "extra" }
        """;

    public const string NotJsonAnswer = "I found one pump.";

    public static ExtractionInput CreateInput(bool isMachineFile = false, params ConvertedChunk[] chunks) =>
        new()
        {
            Chunks = chunks.Length > 0 ? chunks : [new ConvertedChunk { Location = "page 1", Text = "Pump manual." }],
            Units =
            [
                new MetricUnit { QuantityKind = "temperature", Name = "celsius", Abbreviation = "C" },
                new MetricUnit { QuantityKind = "pressure", Name = "bar", Abbreviation = "bar" },
                new MetricUnit { QuantityKind = "Discrete", Name = "count", Abbreviation = "#" },
            ],
            ExistingMetricKeys = ["motor.temperature"],
            ExistingStatusCodes = ["E12", "E13"],
            IsMachineFile = isMachineFile,
        };
}
