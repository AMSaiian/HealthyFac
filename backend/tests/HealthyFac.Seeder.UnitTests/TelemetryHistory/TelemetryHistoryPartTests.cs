using System.Text.Json.Nodes;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.UnitTests.TelemetryHistory.Data;

namespace HealthyFac.Seeder.UnitTests.TelemetryHistory;

public sealed class TelemetryHistoryPartTests
{
    [Fact(Explicit = true)]
    public void ReadEvents_WithASmallScript_ListsTheStepsTheMaintenanceAndTheMarker()
    {
        var events = CreatePart().ReadEvents(TelemetryHistoryPartTestData.Folder());

        events.Count(seedEvent => seedEvent.Item.StartsWith(TelemetryHistoryPartTestData.StepPrefix))
            .Should()
            .Be(TelemetryHistoryPartTestData.StepCount);

        events.Select(seedEvent => seedEvent.Item)
            .Should()
            .Contain(
            [
                TelemetryHistoryPartTestData.MaintenanceStartItem,
                TelemetryHistoryPartTestData.MaintenanceEndItem,
            ]);

        events[^1].Item.Should().Be(TelemetryHistoryPartTestData.MarkerItem);
        events[0].At.ToString().Should().Be(expected: "h-2");
        events[^1].At.ToString().Should().Be(expected: "h-0");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAMaintenanceMoment_ListsItsStepBeforeTheStateChange()
    {
        var items = CreatePart()
            .ReadEvents(TelemetryHistoryPartTestData.Folder())
            .Select(seedEvent => seedEvent.Item)
            .ToList();

        items.IndexOf($"{TelemetryHistoryPartTestData.StepPrefix}h-1:05")
            .Should()
            .BeGreaterThan(expected: -1)
            .And.BeLessThan(items.IndexOf(TelemetryHistoryPartTestData.MaintenanceStartItem));

        items.IndexOf($"{TelemetryHistoryPartTestData.StepPrefix}h-0:50")
            .Should()
            .BeGreaterThan(expected: -1)
            .And.BeLessThan(items.IndexOf(TelemetryHistoryPartTestData.MaintenanceEndItem));
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAWorkOrderOnADrain_ListsItsEventsAndTheLinkAtTheirMoments()
    {
        var folder = TelemetryHistoryPartTestData.Folder(root => TelemetryHistoryPartTestData.AddWorkOrder(root));

        var events = CreatePart().ReadEvents(folder);

        string[] items =
        [
            TelemetryHistoryPartTestData.WorkOrderCreateItem,
            TelemetryHistoryPartTestData.WorkOrderAssignItem,
            TelemetryHistoryPartTestData.WorkOrderStartItem,
            TelemetryHistoryPartTestData.WorkOrderCompleteItem,
            TelemetryHistoryPartTestData.LinkItem,
        ];

        items.Select(item => events.Single(seedEvent => seedEvent.Item == item).At.ToString())
            .Should()
            .Equal(TelemetryHistoryPartTestData.WorkOrderMoments);
    }

    [Fact(Explicit = true)]
    public void StepMinutes_With51DefinitionsAndTheDefaultBatch_Is3()
    {
        var minutes = TelemetryHistoryPart.StepMinutes(
            sampleIntervalSeconds: 1, TelemetryHistoryPartTestData.CatchUpBatchReadings, definitionCount: 51);

        minutes.Should().Be(expected: 3);
    }

    [Fact(Explicit = true)]
    public void StepMinutes_WithAHugeDefinitionCount_IsAtLeast1()
    {
        var minutes = TelemetryHistoryPart.StepMinutes(
            sampleIntervalSeconds: 1, catchUpBatchReadings: 100, definitionCount: 51);

        minutes.Should().Be(expected: 1);
    }

    [Theory(Explicit = true)]
    [MemberData(nameof(TelemetryHistoryPartTestData.Violations), MemberType = typeof(TelemetryHistoryPartTestData))]
    public void ReadEvents_WithARuleViolation_FailsWithTheFileTheItemAndTheRule(
        string name,
        Action<JsonNode> change,
        string item,
        string rule
    )
    {
        var folder = TelemetryHistoryPartTestData.Folder(change);

        var act = () => CreatePart().ReadEvents(folder);

        act.Should().Throw<SeedFileException>(name).WithMessage($"*{item}*{rule}*");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAnUnknownDraftIdInMachines_FailsWithTheItemOfTheMachine()
    {
        var folder = TelemetryHistoryPartTestData.Folder(
            machinesChange: root => root["machines"]![0]!["draftId"] =
                "0190f3a0-0000-7000-8000-000000000e99");

        var act = () => CreatePart().ReadEvents(folder);

        act.Should()
            .Throw<SeedFileException>()
            .WithMessage(expectedWildcardPattern: "*machines.json#pump.definitions*e99*");
    }

    private static TelemetryHistoryPart CreatePart() =>
        new(null!, () => TelemetryHistoryPartTestData.CatchUpBatchReadings);
}
