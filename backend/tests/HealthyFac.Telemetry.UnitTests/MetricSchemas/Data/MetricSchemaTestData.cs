using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Application.MetricSchemas;
using HealthyFac.Telemetry.Domain.MetricDefinitions;

namespace HealthyFac.Telemetry.UnitTests.MetricSchemas.Data;

public static class MetricSchemaTestData
{
    public const string GatewayId = "abcdefghijklmnop";
    public const int Revision = 3;
    public const string SampledAt = "2026-11-06T10:15:00.000Z";
    public const string TemperatureKey = "spindle.temperature";
    public const string DoorKey = "door.open";
    public const string ModeKey = "spindle.mode";

    public static IReadOnlyList<MetricSchema> Metrics { get; } =
    [
        new()
        {
            Key = TemperatureKey,
            Name = "Spindle temperature",
            ValueKind = MetricValueKind.Numeric,
            UnitQuantity = "Temperature",
            UnitName = "DegreeCelsius",
            Min = -20,
            Max = 200,
        },
        new() { Key = DoorKey, Name = "Door open", ValueKind = MetricValueKind.Boolean },
        new()
        {
            Key = ModeKey,
            Name = "Spindle mode",
            ValueKind = MetricValueKind.StateSet,
            States = ["auto", "manual", "off"],
        },
    ];

    public static JsonObject CreateMessage(
        double temperature = 71.5,
        bool isDoorOpen = true,
        string mode = "auto",
        string[]? statuses = null
    ) =>
        new()
        {
            [propertyName: "sampledAt"] = SampledAt,
            [propertyName: "metrics"] = new JsonObject
            {
                [TemperatureKey] = temperature,
                [DoorKey] = isDoorOpen,
                [ModeKey] = mode,
            },
            [propertyName: "statuses"] = new JsonArray((statuses ?? ["E042"]).Select(code => (JsonNode)code).ToArray()),
        };

    public static JsonObject CreateEmptyMessage() =>
        new()
        {
            [propertyName: "sampledAt"] = SampledAt, [propertyName: "metrics"] = new JsonObject(),
            [propertyName: "statuses"] = new JsonArray(),
        };

    public static TheoryData<double, string> ValuesOutsideRangeAndStates => new()
    {
        { -20.1, "auto" },
        { 200.1, "auto" },
        { 71.5, "unknown" },
        { 1e9, "AUTO" },
    };

    public static TheoryData<string> BrokenMessages => new()
    {
        Message(metrics: JsonMetrics("\"spindle.temperature\":\"71.5\"", DoorTrue, ModeAuto)),
        Message(metrics: JsonMetrics(Temperature, "\"door.open\":\"yes\"", ModeAuto)),
        Message(metrics: JsonMetrics(Temperature, DoorTrue, "\"spindle.mode\":1")),
        Message(metrics: JsonMetrics(Temperature, DoorTrue, ModeAuto, "\"extra\":1")),
        Message(metrics: JsonMetrics(Temperature, DoorTrue)),
        Message(statuses: null),
        Message(metrics: null),
        Message(sampledAt: null),
        Message(extra: "\"gatewayId\":\"abc\""),
        Message(sampledAt: "\"yesterday\""),
        Message(sampledAt: "20261106"),
        Message(statuses: "[42]"),
        Message(metrics: "[]"),
    };

    private const string Temperature = "\"spindle.temperature\":71.5";
    private const string DoorTrue = "\"door.open\":true";
    private const string ModeAuto = "\"spindle.mode\":\"auto\"";

    private const string ValidMetricsJson =
        "{\"spindle.temperature\":71.5,\"door.open\":true,\"spindle.mode\":\"auto\"}";

    private static string JsonMetrics(params string[] members) => "{" + string.Join(separator: ",", members) + "}";

    private static string Message(
        string? sampledAt = "\"2026-11-06T10:15:00Z\"",
        string? metrics = ValidMetricsJson,
        string? statuses = "[]",
        string extra = ""
    )
    {
        var parts = new List<string>();

        if (sampledAt is not null)
        {
            parts.Add($"\"sampledAt\":{sampledAt}");
        }

        if (extra.Length > 0)
        {
            parts.Add(extra.TrimEnd(trimChar: ','));
        }

        if (metrics is not null)
        {
            parts.Add($"\"metrics\":{metrics}");
        }

        if (statuses is not null)
        {
            parts.Add($"\"statuses\":{statuses}");
        }

        return "{" + string.Join(separator: ",", parts) + "}";
    }
}
