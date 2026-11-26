using HealthyFac.Seeder.Timeline;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.UnitTests.Timeline;

[Feature(TestFeatures.Seeding)]
public sealed class SeedClockTests
{
    private static readonly DateTimeOffset Moment = new(
        year: 2027, month: 3, day: 24, hour: 7, minute: 0, second: 0, TimeSpan.Zero);

    [Fact(Explicit = true)]
    public void GetUtcNow_AfterEnter_ReturnsTheMoment()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");

        var first = clock.GetUtcNow();

        first.Should().Be(Moment);
    }

    [Fact(Explicit = true)]
    public void GetUtcNow_OnLaterReads_AddsOneMicrosecondEachTime()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");

        var reads = new[] { clock.GetUtcNow(), clock.GetUtcNow(), clock.GetUtcNow() };

        reads.Should().Equal(Moment, Moment.AddTicks(ticks: 10), Moment.AddTicks(ticks: 20));
    }

    [Fact(Explicit = true)]
    public void GetUtcNow_OnManyReads_ReturnsDistinctValues()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");

        var reads = Enumerable.Range(start: 0, count: 1000).Select(_ => clock.GetUtcNow()).ToList();

        reads.Distinct().Should().HaveCount(expected: 1000);
    }

    [Fact(Explicit = true)]
    public void GetUtcNow_AfterEnterWithTheSameMoment_ContinuesAfterTheLastRead()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");
        var firstRead = clock.GetUtcNow();
        clock.GetUtcNow();
        clock.GetUtcNow();
        clock.Enter(Moment, item: "orders.json#order-2");

        var read = clock.GetUtcNow();

        read.Should().Be(firstRead.AddTicks(ticks: 30));
    }

    [Fact(Explicit = true)]
    public void Enter_WithAMomentEarlierThanThePreviousOne_ThrowsWithTheItem()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");
        clock.GetUtcNow();
        clock.Enter(Moment.AddHours(hours: 2), item: "orders.json#order-2");

        var act = () => clock.Enter(Moment.AddHours(hours: 1), item: "orders.json#order-3");

        act.Should().Throw<InvalidOperationException>().WithMessage(expectedWildcardPattern: "*orders.json#order-3*");
    }

    [Fact(Explicit = true)]
    public void Enter_WithAnEarlierMoment_Throws()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");
        clock.GetUtcNow();

        var act = () => clock.Enter(Moment.AddHours(hours: -1), item: "orders.json#order-2");

        act.Should().Throw<InvalidOperationException>().WithMessage(expectedWildcardPattern: "*orders.json#order-2*");
    }

    [Fact(Explicit = true)]
    public void GetUtcNow_AfterEnterWithALaterMoment_ReturnsThatMoment()
    {
        var clock = new SeedClock();
        clock.Enter(Moment, item: "orders.json#order-1");
        clock.GetUtcNow();
        clock.Enter(Moment.AddHours(hours: 1), item: "orders.json#order-2");

        var read = clock.GetUtcNow();

        read.Should().Be(Moment.AddHours(hours: 1));
    }

    [Fact(Explicit = true)]
    public void GetUtcNow_BeforeTheFirstEnter_Throws()
    {
        var clock = new SeedClock();

        var act = clock.GetUtcNow;

        act.Should().Throw<InvalidOperationException>().WithMessage(TimelineErrors.ClockNotEntered);
    }
}
