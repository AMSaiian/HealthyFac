using System.Text.Json.Nodes;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;

namespace HealthyFac.Seeder.UnitTests.TelemetryHistory.Data;

public static class TelemetryHistoryPartTestData
{
    public const string FileName = "telemetry.json";
    public const string StepPrefix = "telemetry.json#step.";
    public const string MarkerItem = "telemetry.json#marker";
    public const string MaintenanceStartItem = "telemetry.json#lathe-spindle.maintenance.inMaintenance";
    public const string MaintenanceEndItem = "telemetry.json#lathe-spindle.maintenance.operational";
    public const int CatchUpBatchReadings = 10000;
    public const string WorkOrderCreateItem = "telemetry.json#lathe-spindle-work-order.created";
    public const string WorkOrderAssignItem = "telemetry.json#lathe-spindle-work-order.assigned";
    public const string WorkOrderStartItem = "telemetry.json#lathe-spindle-work-order.status.0.inProgress";
    public const string WorkOrderCompleteItem = "telemetry.json#lathe-spindle-work-order.status.1.completed";
    public const string LinkItem = "telemetry.json#lathe-spindle.link";

    /// <summary>The moments of the work order events: create, assign, start, complete, link.</summary>
    public static readonly string[] WorkOrderMoments = ["h-1:50", "h-1:45", "h-1:05", "h-0:50", "h-1:06"];

    public static void AddWorkOrder(JsonNode root, string createdAt = "h-1:50")
    {
        root[propertyName: "drains"]![index: 0]![propertyName: "workOrder"] = new JsonObject
        {
            [propertyName: "title"] = "Replace the bearings",
            [propertyName: "description"] = "<p>Replace the front bearings.</p>",
            [propertyName: "priority"] = "high",
            [propertyName: "category"] = "repair",
            [propertyName: "estimateMinutes"] = 60,
            [propertyName: "createdAt"] = createdAt,
        };
    }

    /// <summary>The window of 2 hours with S = 3: 40 grid steps, h-0, 2 state changes, 2 maintenance moments.</summary>
    public const int StepCount = 45;

    private const string BaseJson = """
                                    {
                                      "windowHours": 2,
                                      "sampleIntervalSeconds": 1,
                                      "randomSeed": 7,
                                      "markerMessageId": "0190f3a0-0000-7000-8000-000000000d01",
                                      "drains": [
                                        {
                                          "key": "lathe-spindle", "machine": "lathe", "module": "spindle",
                                        "from": "h-2", "drainHours": 1,
                                          "maintenance": { "by": "maksym", "from": "h-1:05", "until": "h-0:50" }
                                        }
                                      ],
                                      "steps": [
                                        {
                                          "key": "cabinet-step", "machine": "lathe", "metric": "cabinet.temperature",
                                        "offset": 5,
                                          "from": "h-1:30", "until": "h-1:00"
                                        }
                                      ],
                                      "gaps": [
                                    { "key": "pump-gap", "machine": "pump", "from": "h-0:40", "until": "h-0:30" }
                                    ],
                                      "overrides": [
                                        {
                                          "key": "compressor-override", "machine": "compressor",
                                          "values": { "drain.fault": true, "compressor.state": "fault" }, "codes": [],
                                          "from": "h-0:20", "until": "h-0:10"
                                        }
                                      ]
                                    }
                                    """;

    /// <summary>A name, a change of the file, the item of the failure, and a part of the rule text.</summary>
    public static TheoryData<string, Action<JsonNode>, string, string> Violations =>
        new()
        {
            {
                "unknown machine", root => root[propertyName: "steps"]![index: 0]![propertyName: "machine"] = "nobody",
                "telemetry.json#cabinet-step", "nobody"
            },
            {
                "unknown module", root => root[propertyName: "drains"]![index: 0]![propertyName: "module"] = "coolantX",
                "telemetry.json#lathe-spindle", "coolantX"
            },
            {
                "unknown metric", root => root[propertyName: "steps"]![index: 0]![propertyName: "metric"] = "no.metric",
                "telemetry.json#cabinet-step", "no.metric"
            },
            {
                "unknown person",
                root => root[propertyName: "drains"]![index: 0]![propertyName: "maintenance"]![propertyName: "by"] =
                    "nobody",
                "telemetry.json#lathe-spindle", "nobody"
            },
            {
                "moment before the window",
                root => root[propertyName: "steps"]![index: 0]![propertyName: "from"] = "h-3",
                "telemetry.json#cabinet-step", "outside the window"
            },
            {
                "moment after the anchor",
                root => root[propertyName: "gaps"]![index: 0]![propertyName: "until"] = "h+1",
                "telemetry.json#pump-gap", "outside the window"
            },
            {
                "moment that is not in hours",
                root => root[propertyName: "steps"]![index: 0]![propertyName: "from"] = "d-1",
                "telemetry.json#cabinet-step", "hour form"
            },
            {
                "end not after start",
                root => root[propertyName: "gaps"]![index: 0]![propertyName: "until"] = "h-0:40",
                "telemetry.json#pump-gap", "ends at or before"
            },
            {
                "drain hours zero", root => root[propertyName: "drains"]![index: 0]![propertyName: "drainHours"] = 0,
                "telemetry.json#lathe-spindle", "DrainHours above 0"
            },
            {
                "maintenance before 80 percent",
                root => root[propertyName: "drains"]![index: 0]![propertyName: "maintenance"]![propertyName: "from"] =
                    "h-1:30",
                "telemetry.json#lathe-spindle", "before 80 %"
            },
            {
                "maintenance at the end of the drain",
                root => root[propertyName: "drains"]![index: 0]![propertyName: "maintenance"]![propertyName: "from"] =
                    "h-1",
                "telemetry.json#lathe-spindle", "or at or after the end"
            },
            {
                "step on a state metric",
                root =>
                {
                    root[propertyName: "steps"]![index: 0]![propertyName: "machine"] = "compressor";
                    root[propertyName: "steps"]![index: 0]![propertyName: "metric"] = "compressor.state";
                },
                "telemetry.json#cabinet-step", "not numeric"
            },
            {
                "state outside the set",
                root => root[propertyName: "overrides"]![index: 0]![propertyName: "values"]![
                    propertyName: "compressor.state"] = "bogus",
                "telemetry.json#compressor-override", "compressor.state"
            },
            {
                "number for a boolean",
                root => root[propertyName: "overrides"]![index: 0]![propertyName: "values"]![
                    propertyName: "drain.fault"] = 1,
                "telemetry.json#compressor-override", "drain.fault"
            },
            {
                "status code that the draft does not have",
                root => root[propertyName: "overrides"]![index: 0]![propertyName: "codes"] =
                    new JsonArray("S0002"),
                "telemetry.json#compressor-override", "S0002"
            },
            {
                "status code in another case",
                root =>
                {
                    var overrideSeed = root[propertyName: "overrides"]![index: 0]!;
                    overrideSeed[propertyName: "machine"] = "lathe";
                    overrideSeed[propertyName: "values"] = new JsonObject();
                    overrideSeed[propertyName: "codes"] = new JsonArray("a112");
                },
                "telemetry.json#compressor-override", "a112"
            },
            {
                "key shared by 2 lists",
                root =>
                {
                    root[propertyName: "drains"]![index: 0]![propertyName: "key"] = "dup";
                    root[propertyName: "gaps"]![index: 0]![propertyName: "key"] = "dup";
                },
                "telemetry.json#dup", "drains and gaps"
            },
            {
                "work order created after the link moment",
                root => AddWorkOrder(root, createdAt: "h-1:03"),
                "telemetry.json#lathe-spindle-work-order", "before the link"
            },
            {
                "work order without a maintenance",
                root =>
                {
                    AddWorkOrder(root);
                    ((JsonObject)root[propertyName: "drains"]![index: 0]!).Remove(propertyName: "maintenance");
                },
                "telemetry.json", "maintenance"
            },
            {
                "window of 0 hours", root => root[propertyName: "windowHours"] = 0,
                "telemetry.json#windowHours", "above 0"
            },
            {
                "interval of 0 seconds", root => root[propertyName: "sampleIntervalSeconds"] = 0,
                "telemetry.json#sampleIntervalSeconds", "above 0"
            },
        };

    public static SeedFolder Folder(Action<JsonNode>? change = null, Action<JsonNode>? machinesChange = null)
    {
        var target = CommittedSeedTestData.CopyCommitted();
        var root = JsonNode.Parse(BaseJson)!;
        change?.Invoke(root);
        File.WriteAllText(Path.Combine(target, FileName), root.ToJsonString());
        if (machinesChange is not null)
        {
            var machinesPath = Path.Combine(target, CommittedSeedTestData.MachinesFile);
            var machines = JsonNode.Parse(File.ReadAllText(machinesPath))!;
            machinesChange(machines);
            File.WriteAllText(machinesPath, machines.ToJsonString());
        }

        return new SeedFolder(target);
    }
}
