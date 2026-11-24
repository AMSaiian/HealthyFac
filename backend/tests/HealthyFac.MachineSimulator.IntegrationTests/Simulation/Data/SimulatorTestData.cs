using System.Text.Json;
using HealthyFac.MachineSimulator.Scenarios;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.Domain.StatusDefinitions;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;

public static class SimulatorTestData
{
    public const string CoreSettingsFile = "appsettings.Core.Test.json";
    public const string TelemetrySettingsFile = "appsettings.Telemetry.Test.json";
    public const string ConnectionStringKey = "ConnectionStrings:Database";
    public const string EnvironmentName = "Test";
    public const string ApplicationName = "HealthyFac.MachineSimulator.IntegrationTests";
    public const string CoreDatabaseName = "healthyfac_core";
    public const string TelemetryDatabaseName = "healthyfac_telemetry";
    public const string ReadOnlyOptions = "-c default_transaction_read_only=on";
    public const string ReadOnlySqlState = "25006";
    public const string CompanyName = "Simulator Company";
    public const string CompanyTimeZoneId = "Europe/Kyiv";

    public static readonly DateOnly GoLiveDay = new(year: 2026, month: 12, day: 1);

    public static readonly DateTime GoLiveStart = new(
        year: 2026, month: 11, day: 30, hour: 22, minute: 0, second: 0, DateTimeKind.Utc);

    private const string ProposeMetricTool = "propose_metric";
    private const string ProposeStatusTool = "propose_status";
    private const string ProposeMachineTool = "propose_machine";
    public const string GenericSerialNumber = "GEN-0001";
    public const string GenericMetricKey = "level";
    public const string TopicFilter = "healthyfac/gateways/+/messages";
    public const string HealthReadyRoute = "/health/ready";
    public const int HealthPort = 8090;
    public const int RandomSeed = 20261116;
    public const double StoredCounterValue = 100.5;
    public const string CounterKey = "machine.operating_hours";
    public const string PasswordMarker = "Password=";
    public const string HealthyBody = "Healthy";
    public const string UnhealthyBody = "Unhealthy";
    public const string DisconnectWarning = "The MQTT connection to the broker closed";
    public const string FoundMachineInformation = "Found the machine";
    public const string DroppedOutcome = "dropped";
    public const string PublishedOutcome = "published";

    public static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(seconds: 30);

    public static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(seconds: 1);

    public static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(seconds: 2);

    private static readonly string DemoIngestFile = Path.Combine(
        [AppContext.BaseDirectory, "seed", "demo", "ingest", "set-1.json"]);

    public static TheoryData<string, ScenarioKind> DemoMachines =>
        new()
        {
            { ScenarioBinding.CncSerialNumber, ScenarioKind.CncTurningCenter },
            { ScenarioBinding.PumpSerialNumber, ScenarioKind.CentrifugalPump },
            { ScenarioBinding.CompressorSerialNumber, ScenarioKind.ScrewCompressor },
        };

    public static TheoryData<string> AllSerialNumbers =>
        new()
        {
            ScenarioBinding.CncSerialNumber,
            ScenarioBinding.PumpSerialNumber,
            ScenarioBinding.CompressorSerialNumber,
            GenericSerialNumber,
        };

    public static DemoDefinitions ReadDemoDefinitions(string serialNumber)
    {
        if (serialNumber == GenericSerialNumber)
        {
            return new DemoDefinitions(
                [
                    new CreatedMetricDefinition
                    {
                        Key = GenericMetricKey,
                        Name = "Level",
                        ValueKind = MetricValueKind.Numeric,
                        Unit = new MetricUnitReference { QuantityKind = "Temperature", Name = "DegreeCelsius" },
                        Min = 0,
                        Max = 10,
                    },
                ],
                []);
        }

        using var document = JsonDocument.Parse(File.ReadAllText(DemoIngestFile));
        var calls = document.RootElement.GetProperty(propertyName: "entries")
            .EnumerateArray()
            .Select(CollectToolCalls)
            .Single(entryCalls => entryCalls.Any(call => IsMachineOf(call, serialNumber)));
        var metrics = calls
            .Where(call => ToolNameOf(call) == ProposeMetricTool)
            .Select(call => call.GetProperty(propertyName: "arguments"))
            .Where(IsAppliedWithoutReview)
            .Select(ToMetric)
            .ToList();
        var statuses = calls
            .Where(call => ToolNameOf(call) == ProposeStatusTool)
            .Select(call => call.GetProperty(propertyName: "arguments"))
            .Select(draft => (
                Code: draft.GetProperty(propertyName: "code").GetString()!,
                Severity: StatusSeverity.FromValue(draft.GetProperty(propertyName: "severity").GetString()!)))
            .ToList();

        return new DemoDefinitions(metrics, statuses);
    }

    private static string? ToolNameOf(JsonElement call) => call.GetProperty(propertyName: "toolName").GetString();

    private static bool IsMachineOf(JsonElement call, string serialNumber) =>
        ToolNameOf(call) == ProposeMachineTool
        && call.GetProperty(propertyName: "arguments").GetProperty(propertyName: "serialNumber").GetString()
        == serialNumber;

    private static List<JsonElement> CollectToolCalls(JsonElement entry)
    {
        var calls = new List<JsonElement>();
        var pending = new Stack<JsonElement>([entry]);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (node.ValueKind == JsonValueKind.Array)
            {
                node.EnumerateArray().ToList().ForEach(pending.Push);
            }
            else if (node.ValueKind == JsonValueKind.Object)
            {
                if (node.TryGetProperty(propertyName: "toolName", out _))
                {
                    calls.Add(node);
                }

                node.EnumerateObject().ToList().ForEach(property => pending.Push(property.Value));
            }
        }

        return calls;
    }

    private static bool IsAppliedWithoutReview(JsonElement draft) =>
        draft.TryGetProperty(propertyName: "valueKind", out var valueKindElement)
        && valueKindElement.GetString() is { } valueKind
        && (valueKind != MetricValueKind.Numeric.Value
            || (draft.TryGetProperty(propertyName: "quantityKind", out var quantityKind)
                && quantityKind.ValueKind == JsonValueKind.String));

    private static CreatedMetricDefinition ToMetric(JsonElement draft)
    {
        var kind = MetricValueKind.FromValue(draft.GetProperty(propertyName: "valueKind").GetString()!);
        var hasQuantityKind = draft.TryGetProperty(propertyName: "quantityKind", out var quantityKind);

        return new CreatedMetricDefinition
        {
            Key = draft.GetProperty(propertyName: "key").GetString()!,
            Name = draft.GetProperty(propertyName: "name").GetString()!,
            ValueKind = kind,
            Unit = hasQuantityKind
                ? new MetricUnitReference
                {
                    QuantityKind = quantityKind.GetString()!,
                    Name = draft.GetProperty(propertyName: "unit").GetString()!,
                }
                : kind == MetricValueKind.Numeric
                    ? throw MissingNumericUnit(draft)
                    : null,
            Min = ReadNumber(draft, name: "min"),
            Max = ReadNumber(draft, name: "max"),
            States = draft.TryGetProperty(propertyName: "states", out var states)
                && states.ValueKind == JsonValueKind.Array
                    ? states.EnumerateArray().Select(item => item.GetString()!).ToList()
                    : null,
        };
    }

    private static InvalidOperationException MissingNumericUnit(JsonElement draft) =>
        new($"The numeric demo draft {draft.GetProperty(propertyName: "key").GetString()} has no unit.");

    private static double? ReadNumber(JsonElement draft, string name) =>
        draft.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDouble()
            : null;
}

public sealed record DemoDefinitions(
    IReadOnlyList<CreatedMetricDefinition> Metrics,
    IReadOnlyList<(string Code, StatusSeverity Severity)> Statuses
);
