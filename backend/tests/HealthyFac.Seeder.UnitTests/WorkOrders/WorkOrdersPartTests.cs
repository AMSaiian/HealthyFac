using System.Text.Json.Nodes;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;
using HealthyFac.Seeder.WorkOrders;

namespace HealthyFac.Seeder.UnitTests.WorkOrders;

public sealed class WorkOrdersPartTests
{
    private const string OrderKey = "coolant-check";

    [Fact(Explicit = true)]
    public void ReadEvents_WithTheSameStatusTwiceInOneOrder_GivesEachChangeItsOwnItem()
    {
        var folder = CreateFolder(("inProgress", "sofiia"), ("blocked", "sofiia"), ("inProgress", "nadiia"));
        var part = new WorkOrdersPart(null!);

        var events = part.ReadEvents(folder);

        var items = events.Select(seedEvent => seedEvent.Item)
            .Where(item => item.StartsWith($"work-orders.json#{OrderKey}.status"))
            .ToList();

        items.Should().OnlyHaveUniqueItems().And.HaveCount(expected: 3);
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAnUnknownActorOfAStatusChange_NamesTheStatusItem()
    {
        var folder = CreateFolder(("inProgress", "ghost"));
        var part = new WorkOrdersPart(null!);

        var act = () => part.ReadEvents(folder);

        act.Should()
            .Throw<SeedFileException>()
            .WithMessage($"*work-orders.json#{OrderKey}.status.0.inProgress*ghost*");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAnHourPlannedStartInTheFile_FailsWithTheItemAndTheTimeOfDayRule()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.WorkOrdersFile,
            root => CommittedSeedTestData.FindOrder(root, OrderKey)["assignment"]![
                "plannedStart"] = "h-2");

        var part = new WorkOrdersPart(null!);

        var act = () => part.ReadEvents(folder);

        act.Should()
            .Throw<SeedFileException>()
            .WithMessage($"*work-orders.json#{OrderKey}.assigned*needs a planned start with a time of day*");
    }

    private static SeedFolder CreateFolder(params (string Status, string By)[] changes) =>
        CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.WorkOrdersFile,
            root =>
            {
                var statusChanges = CommittedSeedTestData.FindOrder(root, OrderKey)["statusChanges"]!
                    .AsArray();

                for (var index = 0; index < changes.Length; index++)
                {
                    statusChanges.Add(
                        new JsonObject
                        {
                            ["at"] = $"wd-1 15:{10 + index * 10}",
                            ["by"] = changes[index].By,
                            ["status"] = changes[index].Status,
                        });
                }
            });
}
