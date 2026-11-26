using System.Text.Json.Nodes;
using HealthyFac.Seeder.Cases;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;
using HealthyFac.Seeder.WorkOrders;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.UnitTests.SeedFiles;

[Feature(TestFeatures.Seeding)]
public sealed class CommittedSeedCheckTests
{
    private const int CatchUpBatchReadings = 10000;

    [Theory(Explicit = true)]
    [MemberData(nameof(CommittedSeedTestData.Anchors), MemberType = typeof(CommittedSeedTestData))]
    public async Task Run_OnTheCommittedSeed_ReturnsNoFailureForAnyAnchorDay(string name, DateTimeOffset anchor)
    {
        var folder = CommittedSeedTestData.CommittedFolder();
        var events = LoadEvents(folder);

        var failures = await RunCheckAsync(folder, events, anchor);

        failures.Should().BeEmpty(name);
    }

    [Theory(Explicit = true)]
    [MemberData(nameof(CommittedSeedTestData.Anchors), MemberType = typeof(CommittedSeedTestData))]
    public async Task Run_InModeTwoOnTheCommittedSeed_ReturnsNoFailureForAnyAnchorDay(
        string name,
        DateTimeOffset anchor
    )
    {
        var folder = CommittedSeedTestData.CommittedFolder();
        var events = LoadEvents(folder, isTelemetry: true);

        var failures = await RunCheckAsync(folder, events, anchor, isTelemetry: true);

        failures.Should().BeEmpty(name);
    }

    [Fact(Explicit = true)]
    public async Task Run_InModeTwoWithAStepInsideAMaintenanceOfItsMachine_ReturnsTheItemOfTheStep()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.TelemetryFile,
            root =>
            {
                var step = root["steps"]![0]!;
                step["from"] = "h-40";
                step["until"] = "h-39";
            });

        var events = LoadEvents(folder, isTelemetry: true);

        var failures = await RunCheckAsync(folder, events, CommittedSeedTestData.WednesdayAnchor, isTelemetry: true);

        failures.Should()
            .ContainSingle(failure => failure.Item == "telemetry.json#compressor-element"
                && failure.Rule.Contains("overlaps a maintenance"));
    }

    [Fact(Explicit = true)]
    public async Task Run_InModeTwoWithAMaintenanceInsideAnotherOfTheSameMachine_ReturnsTheItemOfTheChange()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.MachinesFile,
            root =>
            {
                var changes = root["stateChanges"]!.AsArray();
                changes.Add(PumpChange(at: "wd-1 10:00", state: "inMaintenance"));
                changes.Add(PumpChange(at: "wd+0 06:00", state: "operational"));
            });

        var events = LoadEvents(folder, isTelemetry: true);

        var failures = await RunCheckAsync(folder, events, CommittedSeedTestData.WednesdayAnchor, isTelemetry: true);

        failures.Should()
            .Contain(failure => failure.Item == "telemetry.json#pump-bearings.maintenance.inMaintenance"
                && failure.Rule.Contains("alternate"));
    }

    [Fact(Explicit = true)]
    public async Task Run_OnACopyWithAnUnknownKey_ReturnsTheKeyNotCreatedFailure()
    {
        var folder = CommittedSeedTestData.CopyWith(
            CommittedSeedTestData.MachinesFile,
            root => root["machines"]![0]!["appliedBy"] = "nobody");

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
            root => CommittedSeedTestData.FindOrder(root, key: "wear-rings")["createdAt"] = "wd+2 15:00");

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
                CommittedSeedTestData.FindOrder(root, key: "pump-seal")["assignment"]!
                    ["at"] = "wd-7 15:05");

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
            root => CommittedSeedTestData.FindOrder(root, key: "wear-rings")["createdAt"] = "yesterday");

        var act = () => LoadEvents(folder);

        act.Should().Throw<SeedFileException>().WithMessage($"*{CommittedSeedTestData.WorkOrdersFile}*");
    }

    private static JsonNode PumpChange(string at, string state) =>
        new JsonObject
        {
            ["machine"] = "pump",
            ["changedBy"] = "maksym",
            ["at"] = at,
            ["state"] = state,
        };

    private static IReadOnlyList<SeedEvent> LoadEvents(SeedFolder folder, bool isTelemetry = false)
    {
        ISeedPart[] parts =
        [
            new PeoplePart(null!, null!),
            new IngestPart(null!),
            .. isTelemetry ? new ISeedPart[] { new TelemetryHistoryPart(null!, () => CatchUpBatchReadings) } : [],
            new MachinesPart(null!),
            new CasesPart(null!),
            new WorkOrdersPart(null!),
        ];

        return [.. SeedPlan.Load(parts, folder).Select(planned => planned.Event)];
    }

    private static Task<IReadOnlyList<SeedCheckFailure>> RunCheckAsync(
        SeedFolder folder,
        IReadOnlyList<SeedEvent> events,
        DateTimeOffset anchor,
        bool isTelemetry = false
    )
    {
        var check = new SeedCheck(
            anchor,
            CommittedSeedTestData.Zone,
            SeedCheckTestData.ValidateAsync,
            SeedCompletion.FindMarkerItem(folder),
            isTelemetry
                ? new TelemetryScript
                {
                    Telemetry = folder.Read<TelemetryFile>(TelemetryHistoryConstants.FileName),
                    Machines = folder.Read<MachinesFile>(MachinesConstants.FileName),
                }
                : null);

        return check.RunAsync(events, CancellationToken.None);
    }
}
