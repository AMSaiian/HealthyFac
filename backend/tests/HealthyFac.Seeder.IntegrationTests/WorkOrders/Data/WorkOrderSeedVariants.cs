using System.Text.Json.Nodes;
using HealthyFac.Seeder.IntegrationTests.People.Data;

namespace HealthyFac.Seeder.IntegrationTests.WorkOrders.Data;

/// <summary>Creates copies of the committed seed folder with one change, so a test can prove a named failure.</summary>
public static class WorkOrderSeedVariants
{
    private const string WorkOrdersFile = "work-orders.json";
    private const string MachinesFile = "machines.json";

    public static string BearingVibrationOutsideShift() =>
        Create(
            WorkOrdersFile, root =>
                FindOrder(root, key: "bearing-vibration")["assignment"]!["plannedStart"] =
                    "wd+1 07:00");

    public static string PumpCouplingSlotTaken() =>
        Create(
            WorkOrdersFile,
            root =>
            {
                var assignment = FindOrder(root, key: "pump-coupling")["assignment"]!;
                assignment["assignee"] = "petro";
                assignment["at"] = "wd-2 09:00";
                assignment["plannedStart"] = "wd-1 10:00";
                FindOrder(root, key: "pump-coupling")["estimateMinutes"] = 60;
            });

    public static string StatusChangeOnBacklogOrder() =>
        Create(
            WorkOrdersFile,
            root => FindOrder(root, key: "wear-rings")["statusChanges"]!.AsArray()
                .Add(
                    new JsonObject
                    {
                        ["at"] = "d-1 10:00", ["by"] = "taras",
                        ["status"] = "inProgress",
                    }));

    public static string CoolantCheckWithRepeatedStatus() =>
        Create(
            WorkOrdersFile,
            root =>
            {
                var changes = FindOrder(root, key: "coolant-check")["statusChanges"]!.AsArray();

                changes.Add(
                    new JsonObject
                    {
                        ["at"] = "wd-1 15:10", ["by"] = "sofiia",
                        ["status"] = "inProgress",
                    });

                changes.Add(
                    new JsonObject
                    {
                        ["at"] = "wd-1 15:20", ["by"] = "sofiia",
                        ["status"] = "blocked",
                    });

                changes.Add(
                    new JsonObject
                    {
                        ["at"] = "wd-1 15:30", ["by"] = "nadiia",
                        ["status"] = "inProgress",
                    });
            });

    public static string PumpDraftAppliedTwice() =>
        Create(
            MachinesFile,
            root =>
            {
                var machines = root["machines"]!.AsArray();
                var copy = JsonNode.Parse(machines[0]!.ToJsonString())!.AsObject();
                copy["key"] = "pump-again";
                copy["appliedAt"] = "d-14 10:50";
                machines.Add(copy);
            });

    private static JsonNode FindOrder(JsonNode root, string key) =>
        root["workOrders"]!.AsArray()
            .Single(order => order!["key"]!.GetValue<string>() == key)!;

    private static string Create(string fileName, Action<JsonNode> change)
    {
        var source = Path.Combine(AppContext.BaseDirectory, PeopleTestData.SeedFolder);
        var target = Directory.CreateTempSubdirectory().FullName;
        CopyDirectory(source, target);
        var path = Path.Combine(target, fileName);
        var root = JsonNode.Parse(File.ReadAllText(path))!;
        change(root);
        File.WriteAllText(path, root.ToJsonString());

        return target;
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var directory in Directory.GetDirectories(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, target));
        }

        foreach (var file in Directory.GetFiles(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, target));
        }
    }
}
