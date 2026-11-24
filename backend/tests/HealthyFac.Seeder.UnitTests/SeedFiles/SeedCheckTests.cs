using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;

namespace HealthyFac.Seeder.UnitTests.SeedFiles;

public sealed class SeedCheckTests
{
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(id: "Europe/Kyiv");

    private static SeedCheck CreateScriptCheck() =>
        new(
            SeedCheckTestData.Anchor,
            Kyiv,
            SeedCheckTestData.ValidateAsync,
            SeedCheckTestData.CompletionItem,
            SeedCheckTestData.EmptyScript);

    [Theory(Explicit = true)]
    [MemberData(nameof(SeedCheckTestData.FailingCases), MemberType = typeof(SeedCheckTestData))]
    public async Task Run_WithABrokenRule_ReturnsTheFileTheItemAndTheRule(
        string name,
        IReadOnlyList<SeedEvent> events,
        string item,
        string ruleText
    )
    {
        var check = new SeedCheck(SeedCheckTestData.Anchor, Kyiv, SeedCheckTestData.ValidateAsync);

        var failures = await check.RunAsync(events, CancellationToken.None);

        failures.Should().ContainSingle(failure => failure.Item == item, name);
        var failure = failures.Single(candidate => candidate.Item == item);
        failure.File.Should().Be(SeedItem.FileOf(item));
        failure.Rule.Should().Contain(ruleText);
    }

    [Fact(Explicit = true)]
    public async Task Run_WithAnEventAfterTheCompletionMarkerEvent_ReturnsTheItemOfThatEvent()
    {
        var check = new SeedCheck(
            SeedCheckTestData.Anchor,
            Kyiv,
            SeedCheckTestData.ValidateAsync,
            SeedCheckTestData.CompletionItem);

        var failures = await check.RunAsync(SeedCheckTestData.EventsWithAnEventAfterTheMarker, CancellationToken.None);

        failures.Should().ContainSingle();
        failures[index: 0].Item.Should().Be(SeedCheckTestData.LateItem);
        failures[index: 0].Rule.Should().Contain(SeedCheckTestData.CompletionItem);
    }

    [Fact(Explicit = true)]
    public async Task Run_WithACompletionItemThatNoEventHas_ReturnsAFailureForThatItem()
    {
        var check = new SeedCheck(
            SeedCheckTestData.Anchor,
            Kyiv,
            SeedCheckTestData.ValidateAsync,
            SeedCheckTestData.MissingCompletionItem);

        var failures = await check.RunAsync(SeedCheckTestData.EventsEndingWithTheMarker, CancellationToken.None);

        failures.Should().ContainSingle(failure => failure.Item == SeedCheckTestData.MissingCompletionItem);
    }

    [Fact(Explicit = true)]
    public async Task Run_WithTheCompletionMarkerEventLast_ReturnsNoFailure()
    {
        var check = new SeedCheck(
            SeedCheckTestData.Anchor,
            Kyiv,
            SeedCheckTestData.ValidateAsync,
            SeedCheckTestData.CompletionItem);

        var failures = await check.RunAsync(SeedCheckTestData.EventsEndingWithTheMarker, CancellationToken.None);

        failures.Should().BeEmpty();
    }

    [Fact(Explicit = true)]
    public async Task Run_InModeTwoWithScriptEventsAfterTheModeOneMarker_ReturnsNoFailure()
    {
        var check = CreateScriptCheck();

        var failures = await check.RunAsync(SeedCheckTestData.ScriptEventsAfterTheMarker, CancellationToken.None);

        failures.Should().BeEmpty();
    }

    [Fact(Explicit = true)]
    public async Task Run_InModeTwoWithAModeOneEventAfterTheModeOneMarker_ReturnsTheItemOfThatEvent()
    {
        var check = CreateScriptCheck();

        var failures = await check.RunAsync(
            SeedCheckTestData.ScriptEventsWithAModeOneEventAfterTheMarker, CancellationToken.None);

        failures.Should().ContainSingle();
        failures[index: 0].Item.Should().Be(SeedCheckTestData.LateItem);
        failures[index: 0].Rule.Should().Contain(SeedCheckTestData.CompletionItem);
    }

    [Fact(Explicit = true)]
    public async Task Run_InModeTwoWithAnEventAfterTheScriptMarker_ReturnsTheItemOfThatEvent()
    {
        var check = CreateScriptCheck();

        var failures = await check.RunAsync(
            SeedCheckTestData.ScriptEventsWithAStepAfterTheScriptMarker, CancellationToken.None);

        failures.Should().ContainSingle();
        failures[index: 0].Item.Should().Be(SeedCheckTestData.ScriptLateStepItem);
        failures[index: 0].Rule.Should().Contain(SeedCheckTestData.ScriptMarkerItem);
    }

    [Fact(Explicit = true)]
    public async Task Run_InModeTwoWithoutTheScriptMarker_ReturnsAFailureForTheMarkerItem()
    {
        var check = CreateScriptCheck();

        var failures = await check.RunAsync(SeedCheckTestData.EventsEndingWithTheMarker, CancellationToken.None);

        failures.Should().ContainSingle(failure => failure.Item == SeedCheckTestData.ScriptMarkerItem);
    }

    [Fact(Explicit = true)]
    public async Task Run_WithACleanSet_ReturnsNoFailure()
    {
        var check = new SeedCheck(SeedCheckTestData.Anchor, Kyiv, SeedCheckTestData.ValidateAsync);

        var failures = await check.RunAsync(SeedCheckTestData.CleanEvents, CancellationToken.None);

        failures.Should().BeEmpty();
    }
}
