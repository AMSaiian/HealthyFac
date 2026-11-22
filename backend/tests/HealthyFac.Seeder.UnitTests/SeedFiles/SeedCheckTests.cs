using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;

namespace HealthyFac.Seeder.UnitTests.SeedFiles;

public sealed class SeedCheckTests
{
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(id: "Europe/Kyiv");

    [Theory]
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

    [Fact]
    public async Task Run_WithACleanSet_ReturnsNoFailure()
    {
        var check = new SeedCheck(SeedCheckTestData.Anchor, Kyiv, SeedCheckTestData.ValidateAsync);

        var failures = await check.RunAsync(SeedCheckTestData.CleanEvents, CancellationToken.None);

        failures.Should().BeEmpty();
    }
}
