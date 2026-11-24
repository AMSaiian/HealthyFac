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
                FindOrder(root, key: "bearing-vibration")[propertyName: "assignment"]![propertyName: "plannedStart"] =
                    "wd+1 07:00");

    public static string PumpCouplingSlotTaken() =>
        Create(
            WorkOrdersFile,
            root =>
            {
                var assignment = FindOrder(root, key: "pump-coupling")[propertyName: "assignment"]!;
                assignment[propertyName: "assignee"] = "petro";
                assignment[propertyName: "at"] = "wd-2 09:00";
                assignment[propertyName: "plannedStart"] = "wd-1 10:00";
                FindOrder(root, key: "pump-coupling")[propertyName: "estimateMinutes"] = 60;
            });

    public static string StatusChangeOnBacklogOrder() =>
        Create(
            WorkOrdersFile,
            root => FindOrder(root, key: "wear-rings")[propertyName: "statusChanges"]!.AsArray()
                .Add(
                    new JsonObject
                    {
                        [propertyName: "at"] = "d-1 10:00", [propertyName: "by"] = "taras",
                        [propertyName: "status"] = "inProgress",
                    }));

    public static string CoolantCheckWithRepeatedStatus() =>
        Create(
            WorkOrdersFile,
            root =>
            {
                var changes = FindOrder(root, key: "coolant-check")[propertyName: "statusChanges"]!.AsArray();
                changes.Add(
                    new JsonObject
                    {
                        [propertyName: "at"] = "wd-1 15:10", [propertyName: "by"] = "sofiia",
                        [propertyName: "status"] = "inProgress",
                    });
                changes.Add(
                    new JsonObject
                    {
                        [propertyName: "at"] = "wd-1 15:20", [propertyName: "by"] = "sofiia",
                        [propertyName: "status"] = "blocked",
                    });
                changes.Add(
                    new JsonObject
                    {
                        [propertyName: "at"] = "wd-1 15:30", [propertyName: "by"] = "nadiia",
                        [propertyName: "status"] = "inProgress",
                    });
            });

    public static string PumpDraftAppliedTwice() =>
        Create(
            MachinesFile,
            root =>
            {
                var machines = root[propertyName: "machines"]!.AsArray();
                var copy = JsonNode.Parse(machines[index: 0]!.ToJsonString())!.AsObject();
                copy[propertyName: "key"] = "pump-again";
                copy[propertyName: "appliedAt"] = "d-14 10:50";
                machines.Add(copy);
            });

    private static JsonNode FindOrder(JsonNode root, string key) =>
        root[propertyName: "workOrders"]!.AsArray()
            .Single(order => order![propertyName: "key"]!.GetValue<string>() == key)!;

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
