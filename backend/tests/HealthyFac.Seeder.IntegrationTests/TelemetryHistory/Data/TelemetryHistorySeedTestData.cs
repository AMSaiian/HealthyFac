using HealthyFac.Seeder.IntegrationTests.Timeline.Data;

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

    /// <summary>
    /// Copies the committed seed folder, replaces <c>telemetry.json</c>, and returns the base directory.
    /// </summary>
    /// <param name="script">The content of <c>telemetry.json</c>.</param>
    /// <returns>The path of the base directory with the <c>services</c> and <c>seed/demo</c> folders.</returns>
    public static string CreateBaseDirectory(string script)
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

        return SeedBaseDirectory.Create(folder);
    }
}
