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

    private static readonly string DemoFile = Path.Combine(
        AppContext.BaseDirectory, path2: "demo-ingest", path3: "core.json");

    private static readonly string DemoTelemetryFile = Path.Combine(
        AppContext.BaseDirectory, path2: "demo-ingest", path3: "telemetry.json");

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

        using var document = JsonDocument.Parse(File.ReadAllText(DemoFile));
        using var telemetryDocument = JsonDocument.Parse(File.ReadAllText(DemoTelemetryFile));
        var root = document.RootElement;
        var telemetryRoot = telemetryDocument.RootElement;
        var machine = root.GetProperty(propertyName: "machineDrafts")
            .EnumerateArray()
            .Single(draft => draft.GetProperty(propertyName: "serial_number").GetString() == serialNumber);
        var machineDraftId = machine.GetProperty(propertyName: "id").GetString();
        var targetMachineId = machine.GetProperty(propertyName: "target_machine_id").GetString();

        bool IsOfMachine(JsonElement draft)
        {
            return draft.GetProperty(propertyName: "target_machine_draft_id").GetString() == machineDraftId
                || (targetMachineId is not null
                    && draft.GetProperty(propertyName: "target_machine_id").GetString() == targetMachineId);
        }

        var metrics = telemetryRoot.GetProperty(propertyName: "metricDefinitionDrafts")
            .EnumerateArray()
            .Where(IsOfMachine)
            .Where(IsAppliedWithoutReview)
            .Select(ToMetric)
            .ToList();
        var statuses = telemetryRoot.GetProperty(propertyName: "statusDefinitionDrafts")
            .EnumerateArray()
            .Where(IsOfMachine)
            .Select(draft => (
                Code: draft.GetProperty(propertyName: "code").GetString()!,
                Severity: StatusSeverity.FromValue(draft.GetProperty(propertyName: "severity").GetString()!)))
            .ToList();

        return new DemoDefinitions(metrics, statuses);
    }

    private static bool IsAppliedWithoutReview(JsonElement draft) =>
        draft.GetProperty(propertyName: "value_kind").GetString() is { } valueKind
        && (valueKind != MetricValueKind.Numeric.Value
            || draft.GetProperty(propertyName: "quantity_kind_name").GetString() is not null);

    private static CreatedMetricDefinition ToMetric(JsonElement draft)
    {
        var kind = MetricValueKind.FromValue(draft.GetProperty(propertyName: "value_kind").GetString()!);
        var quantityKind = draft.GetProperty(propertyName: "quantity_kind_name").GetString();

        return new CreatedMetricDefinition
        {
            Key = draft.GetProperty(propertyName: "key").GetString()!,
            Name = draft.GetProperty(propertyName: "name").GetString()!,
            ValueKind = kind,
            Unit = quantityKind is null
                ? kind == MetricValueKind.Numeric ? throw MissingNumericUnit(draft) : null
                : new MetricUnitReference
                {
                    QuantityKind = quantityKind,
                    Name = draft.GetProperty(propertyName: "unit_name").GetString()!,
                },
            Min = ReadNumber(draft, name: "min"),
            Max = ReadNumber(draft, name: "max"),
            States = draft.GetProperty(propertyName: "states").ValueKind == JsonValueKind.Array
                ? draft.GetProperty(propertyName: "states").EnumerateArray().Select(item => item.GetString()!).ToList()
                : null,
        };
    }

    private static InvalidOperationException MissingNumericUnit(JsonElement draft) =>
        new($"The numeric demo draft {draft.GetProperty(propertyName: "key").GetString()} has no unit.");

    private static double? ReadNumber(JsonElement draft, string name) =>
        draft.GetProperty(name).ValueKind == JsonValueKind.Number ? draft.GetProperty(name).GetDouble() : null;
}

public sealed record DemoDefinitions(
    IReadOnlyList<CreatedMetricDefinition> Metrics,
    IReadOnlyList<(string Code, StatusSeverity Severity)> Statuses
);
