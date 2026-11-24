using HealthyFac.Seeder.Cases;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;
using HealthyFac.Seeder.WorkOrders;

namespace HealthyFac.Seeder.UnitTests.SeedFiles;

public sealed class CommittedSeedCheckTests
{
    [Theory(Explicit = true)]
    [MemberData(nameof(CommittedSeedTestData.Anchors), MemberType = typeof(CommittedSeedTestData))]
    public async Task Run_OnTheCommittedSeed_ReturnsNoFailureForAnyAnchorDay(string name, DateTimeOffset anchor)
    {
        var folder = CommittedSeedTestData.CommittedFolder();
        var events = LoadEvents(folder);

        var failures = await RunCheckAsync(folder, events, anchor);

        failures.Should().BeEmpty(name);
    }

    [Fact(Explicit = true)]
    public async Task Run_OnACopyWithAnUnknownKey_ReturnsTheKeyNotCreatedFailure()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.MachinesFile,
            root => root[propertyName: "machines"]![index: 0]![propertyName: "appliedBy"] = "nobody");
        var events = LoadEvents(folder);

        var failures = await RunCheckAsync(folder, events, CommittedSeedTestData.WednesdayAnchor);

        failures.Should()
            .Contain(failure => failure.Item.StartsWith("machines.json#pump") && failure.Rule.Contains("nobody"));
    }

    [Fact(Explicit = true)]
    public async Task Run_OnACopyWithAnEventAfterTheAnchor_ReturnsTheInThePastFailure()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.WorkOrdersFile,
            root => CommittedSeedTestData.FindOrder(root, key: "wear-rings")[propertyName: "createdAt"] = "wd+2 15:00");
        var events = LoadEvents(folder);

        var failures = await RunCheckAsync(folder, events, CommittedSeedTestData.WednesdayAnchor);

        failures.Should()
            .Contain(failure => failure.Item == "work-orders.json#wear-rings.created"
                && failure.Rule.Contains("past"));
    }

    [Fact(Explicit = true)]
    public async Task Run_OnACopyThatUsesAKeyBeforeItsCreatingEvent_ReturnsTheUsedBeforeFailure()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.WorkOrdersFile,
            root =>
                CommittedSeedTestData.FindOrder(root, key: "pump-seal")[propertyName: "assignment"]!
                    [propertyName: "at"] = "wd-7 15:05");
        var events = LoadEvents(folder);

        var failures = await RunCheckAsync(folder, events, CommittedSeedTestData.WednesdayAnchor);

        failures.Should()
            .Contain(failure => failure.Item == "work-orders.json#pump-seal.assigned"
                && failure.Rule.Contains("pump-seal.order"));
    }

    [Fact(Explicit = true)]
    public void ReadEvents_OnACopyWithAnInvalidMoment_FailsWithTheFile()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.WorkOrdersFile,
            root => CommittedSeedTestData.FindOrder(root, key: "wear-rings")[propertyName: "createdAt"] = "yesterday");

        var act = () => LoadEvents(folder);

        act.Should().Throw<SeedFileException>().WithMessage($"*{CommittedSeedTestData.WorkOrdersFile}*");
    }

    private static IReadOnlyList<SeedEvent> LoadEvents(SeedFolder folder)
    {
        ISeedPart[] parts =
        [
            new PeoplePart(null!, null!),
            new IngestPart(null!),
            new MachinesPart(null!),
            new CasesPart(null!),
            new WorkOrdersPart(null!),
        ];

        return SeedPlan.Load(parts, folder).Select(planned => planned.Event).ToList();
    }

    private static Task<IReadOnlyList<SeedCheckFailure>> RunCheckAsync(
        SeedFolder folder,
        IReadOnlyList<SeedEvent> events,
        DateTimeOffset anchor
    )
    {
        var check = new SeedCheck(
            anchor,
            CommittedSeedTestData.Zone,
            SeedCheckTestData.ValidateAsync,
            SeedCompletion.FindMarkerItem(folder));

        return check.RunAsync(events, CancellationToken.None);
    }
}
