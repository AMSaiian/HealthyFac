using System.Text.Json.Nodes;
using HealthyFac.Seeder.SeedFiles;

namespace HealthyFac.Seeder.UnitTests.SeedFiles.Data;

public static class CommittedSeedTestData
{
    public const string WorkOrdersFile = "work-orders.json";
    public const string MachinesFile = "machines.json";
    public const string TelemetryFile = "telemetry.json";

    public static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById(id: "Europe/Kyiv");

    /// <summary>The anchors are local times in Kyiv (UTC+3 in June 2027).</summary>
    public static TheoryData<string, DateTimeOffset> Anchors => new()
    {
        {
            "a Monday at 07:30",
            new DateTimeOffset(year: 2027, month: 5, day: 31, hour: 4, minute: 30, second: 0, TimeSpan.Zero)
        },
        {
            "a Wednesday at 11:00",
            new DateTimeOffset(year: 2027, month: 6, day: 2, hour: 8, minute: 0, second: 0, TimeSpan.Zero)
        },
        {
            "a Friday at 16:30",
            new DateTimeOffset(year: 2027, month: 6, day: 4, hour: 13, minute: 30, second: 0, TimeSpan.Zero)
        },
        {
            "a Saturday at 10:00",
            new DateTimeOffset(year: 2027, month: 6, day: 5, hour: 7, minute: 0, second: 0, TimeSpan.Zero)
        },
    };

    public static readonly DateTimeOffset WednesdayAnchor =
        new(year: 2027, month: 6, day: 2, hour: 8, minute: 0, second: 0, TimeSpan.Zero);

    public static SeedFolder CommittedFolder() => new(FindCommittedPath());

    public static string FindCommittedPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, path2: "seed", path3: "demo");
            if (File.Exists(Path.Combine(candidate, path2: "company.json")))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(message: "The committed seed folder backend/seed/demo was not found.");
    }

    /// <summary>Copies the committed seed folder into a new temporary folder.</summary>
    /// <returns>The path of the copy.</returns>
    public static string CopyCommitted()
    {
        var source = FindCommittedPath();
        var target = Directory.CreateTempSubdirectory().FullName;
        foreach (var directory in Directory.GetDirectories(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, target));
        }

        foreach (var file in Directory.GetFiles(source, searchPattern: "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, target));
        }

        return target;
    }

    public static SeedFolder CopyWith(string fileName, Action<JsonNode> change)
    {
        var target = CopyCommitted();
        var path = Path.Combine(target, fileName);
        var root = JsonNode.Parse(File.ReadAllText(path))!;
        change(root);
        File.WriteAllText(path, root.ToJsonString());

        return new SeedFolder(target);
    }

    public static JsonNode FindOrder(JsonNode root, string key) =>
        root[propertyName: "workOrders"]!.AsArray()
            .Single(order => order![propertyName: "key"]!.GetValue<string>() == key)!;
}
