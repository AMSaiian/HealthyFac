using System.Text.Json.Nodes;
using HealthyFac.Seeder.IntegrationTests.Timeline.Data;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.TelemetryHistory;

namespace HealthyFac.Seeder.IntegrationTests.TelemetryHistory.Data;

public static class TelemetryHistorySeedTestData
{
    public const string TelemetryArgument = "--telemetry";
    public const string ResetArgument = "--reset";
    public const string TelemetryFile = "telemetry.json";
    public const string Pump = "pump";
    public const string Lathe = "lathe";
    public const string Compressor = "compressor";
    public const string ResetTelemetryHint = "--reset --telemetry";
    public const string SpindleSpeed = "spindle.speed";
    public const string Drift = "drift";
    public const string CompressorStepMetric = "element.outlet_temperature";
    public const string DatabaseSizeSql = "SELECT pg_database_size(current_database())";
    public const double BytesPerMegabyte = 1024 * 1024;
    public const string SilentMachine = "silentMachine";
    public const string UnknownMode = "unknownMode";
    public const string Alarm = "alarm";
    public const int CatchUpBatchReadings = 10000;
    public const int SampleIntervalSeconds = 1;
    public const string InMaintenance = "inMaintenance";
    public const string Operational = "operational";
    public const string PumpSpeed = "motor.speed";
    public const string LatheSpeedBeforeFrom = "h-7:10";
    public const string LatheSpeedBeforeUntil = "h-7:09";
    public static readonly TimeSpan StateTolerance = TimeSpan.FromSeconds(seconds: 1);
    public const string Resolved = "resolved";
    public const string CancelItem = "telemetry.json#step.h-7:30";

    public static readonly HistoryExpectation TestExpectation = new()
    {
        WindowHours = 8,
        LatheDrainFrom = "h-7:30",
        LatheMaintenanceFrom = "h-7:02",
        LatheMaintenanceUntil = "h-6:50",
        PumpDrainFrom = "h-6:48",
        PumpMaintenanceFrom = "h-6:21",
        PumpMaintenanceUntil = "h-6:10",
        CompressorStepFrom = "h-6:40",
        CompressorStepUntil = "h-6:30",
        PumpGapFrom = "h-7:45",
        PumpGapUntil = "h-7:40",
        LatheOverrideFrom = "h-7:50",
        LatheOverrideUntil = "h-7:47",
        GapSampleCount = 5 * 60,
    };

    public static readonly HistoryExpectation CommittedExpectation = new()
    {
        WindowHours = 48,
        LatheDrainFrom = "h-30",
        LatheMaintenanceFrom = "h-22:05",
        LatheMaintenanceUntil = "h-20",
        PumpDrainFrom = "h-14",
        PumpMaintenanceFrom = "h-8:05",
        PumpMaintenanceUntil = "h-7",
        CompressorStepFrom = "h-5",
        CompressorStepUntil = "h-4:40",
        PumpGapFrom = "h-36",
        PumpGapUntil = "h-35:50",
        LatheOverrideFrom = "h-44",
        LatheOverrideUntil = "h-43:45",
        GapSampleCount = 10 * 60,
    };

    public static readonly string[] DrainRules = ["threshold", "status", "trend"];

    public static readonly Guid MarkerMessageId = Guid.Parse(input: "019b4c00-0000-7000-8000-00000000d001");

    /// <summary>A Thursday 12:00 in Kyiv (UTC+3 in June): the compressor is operational in the window.</summary>
    public static readonly DateTimeOffset ThursdayAnchor = new(
        year: 2027, month: 6, day: 3, hour: 9, minute: 0, second: 0, TimeSpan.Zero);

    /// <summary>
    /// The window of 8 hours with one event of each kind. Every event ends before the open fake case of cases.json
    /// starts at h-6.
    /// </summary>
    public const string TestScript = """
                                     {
                                       "windowHours": 8,
                                       "sampleIntervalSeconds": 1,
                                       "randomSeed": 7,
                                       "markerMessageId": "019b4c00-0000-7000-8000-00000000d001",
                                       "drains": [
                                         {
                                           "key": "lathe-spindle", "machine": "lathe",
                                           "module": "spindle", "from": "h-7:30", "drainHours": 0.5,
                                           "maintenance": { "by": "maksym", "from": "h-7:02", "until": "h-6:50" }
                                         },
                                         {
                                           "key": "pump-bearings", "machine": "pump",
                                           "module": "bearings", "from": "h-6:48", "drainHours": 0.5,
                                           "maintenance": { "by": "maksym", "from": "h-6:21", "until": "h-6:10" }
                                         }
                                       ],
                                       "steps": [
                                         {
                                           "key": "compressor-element", "machine": "compressor",
                                           "metric": "element.outlet_temperature",
                                           "offset": 12, "from": "h-6:40", "until": "h-6:30"
                                         }
                                       ],
                                       "gaps": [
                                         {
                                           "key": "pump-outage", "machine": "pump", "from": "h-7:45", "until": "h-7:40"
                                         }
                                       ],
                                       "overrides": [
                                         {
                                           "key": "lathe-setup", "machine": "lathe",
                                           "values": { "spindle.mode": "manual" }, "codes": [],
                                           "from": "h-7:50", "until": "h-7:47"
                                         }
                                       ]
                                     }
                                     """;

    /// <summary>The test script with a work order on the lathe drain, created at h-7:25.</summary>
    public static readonly string TestScriptWithWorkOrder = TestScript.Replace(
        oldValue: "\"from\": \"h-7:02\", \"until\": \"h-6:50\" }",
        "\"from\": \"h-7:02\", \"until\": \"h-6:50\" },\n"
        + "\"workOrder\": { \"title\": \"Replace the bearings\", \"description\": \"<p>Replace them.</p>\", "
        + "\"priority\": \"high\", \"category\": \"repair\", \"estimateMinutes\": 60, \"createdAt\": \"h-7:25\" }");

    public static readonly DrainSeed PumpDrain = new()
    {
        Key = "pump-bearings",
        Machine = Pump,
        Module = "bearings",
        From = RelativeMoment.Parse(text: "h-6:48"),
        DrainHours = 0.5,
        Maintenance = new MaintenanceSeed
        {
            By = MaintenanceActor,
            From = RelativeMoment.Parse(text: "h-6:21"),
            Until = RelativeMoment.Parse(text: "h-6:10"),
        },
    };

    public const string LatheWorkOrderTitle = "Replace the spindle bearings";
    public const string PumpWorkOrderTitle = "Replace the pump bearings";

    /// <summary>A Wednesday 21:00 in Kyiv (UTC+3 in June): the pump slot meets a work order of maksym.</summary>
    public static readonly DateTimeOffset WeekdayEveningAnchor = new(
        year: 2027, month: 6, day: 2, hour: 18, minute: 0, second: 0, TimeSpan.Zero);

    public const string WorkOrdersFile = "work-orders.json";
    public const string BusyWorkOrderTitle = "Check the guard doors";

    /// <summary>
    /// A work order of the assignee, assigned after the drain orders, on the first work block of the anchor day.
    /// </summary>
    public const string BusyWorkOrder = """
                                        {
                                          "key": "guard-doors", "title": "Check the guard doors",
                                          "description": "<p>Check them.</p>",
                                          "priority": "low", "category": "inspection", "machine": "lathe",
                                          "estimateMinutes": 60, "createdAt": "h-5:00", "createdBy": "nadiia",
                                          "assignment": {
                                            "at": "h-4:55", "by": "nadiia", "assignee": "maksym",
                                            "plannedStart": "wd+0 08:00"
                                          },
                                          "statusChanges": []
                                        }
                                        """;

    public const string MaintenanceActor = "maksym";
    public const string PumpLinkItem = "telemetry.json#pump-bearings.link";
    public const string DrainWorkOrderTitle = "Replace the bearings";
    public const string LinkMoment = "h-7:03";
    public const string MaintenanceStartMoment = "h-7:02";

    /// <summary>The moments of the history rows of the drain work order: create, assign, start, complete.</summary>
    public static readonly string[] DrainWorkOrderMoments = ["h-7:25", "h-7:20", "h-7:02", "h-6:50"];

    /// <summary>
    /// Copies the committed seed folder, replaces <c>telemetry.json</c>, and returns the base directory.
    /// </summary>
    /// <param name="script">The content of <c>telemetry.json</c>.</param>
    /// <returns>The path of the base directory with the <c>services</c> and <c>seed/demo</c> folders.</returns>
    public static string CreateBaseDirectory(string script, string? extraWorkOrder = null)
    {
        var folder = Directory.CreateTempSubdirectory().FullName;
        var source = Path.Combine(AppContext.BaseDirectory, SeedProgram.SeedFolderName);
        foreach (var directory in Directory.GetDirectories(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, folder));
        }

        foreach (var file in Directory.GetFiles(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, folder));
        }

        File.WriteAllText(Path.Combine(folder, TelemetryFile), script);
        if (extraWorkOrder is not null)
        {
            var path = Path.Combine(folder, WorkOrdersFile);
            var root = JsonNode.Parse(File.ReadAllText(path))!;
            root[propertyName: "workOrders"]!.AsArray().Add(JsonNode.Parse(extraWorkOrder));
            File.WriteAllText(path, root.ToJsonString());
        }

        return SeedBaseDirectory.Create(folder);
    }
}
