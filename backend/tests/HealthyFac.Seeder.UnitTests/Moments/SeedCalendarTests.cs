using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.UnitTests.Moments.Data;

namespace HealthyFac.Seeder.UnitTests.Moments;

public sealed class SeedCalendarTests
{
    private static readonly TimeZoneInfo Kyiv = TimeZoneInfo.FindSystemTimeZoneById(SeedCalendarTestData.KyivZoneId);

    [Theory(Explicit = true)]
    [MemberData(nameof(SeedCalendarTestData.UtcCases), MemberType = typeof(SeedCalendarTestData))]
    public void ToUtc_WithAnchorAndMoment_ReturnsTheInstant(string name, string input, DateTimeOffset expected)
    {
        var (anchor, text) = Split(input);

        var instant = SeedCalendar.ToUtc(RelativeMoment.Parse(text), anchor, Kyiv);

        instant.Should().Be(expected, name);
    }

    [Theory(Explicit = true)]
    [MemberData(nameof(SeedCalendarTestData.RejectedCases), MemberType = typeof(SeedCalendarTestData))]
    public void ToUtc_WithAnUnusableMoment_ThrowsWithTheText(string name, string input)
    {
        var (anchor, text) = Split(input);

        var act = () => SeedCalendar.ToUtc(RelativeMoment.Parse(text), anchor, Kyiv);

        act.Should().Throw<MomentException>(name).WithMessage($"*'{text}'*");
    }

    [Fact(Explicit = true)]
    public void ToDate_WithDayOffset_ReturnsTheLocalDateThatManyDaysBefore()
    {
        var anchor = SeedCalendarTestData.Utc(year: 2027, month: 3, day: 25, hour: 10, minute: 0);

        var date = SeedCalendar.ToDate(RelativeMoment.Parse(text: "d-30"), anchor, Kyiv);

        date.Should().Be(new DateOnly(year: 2027, month: 2, day: 23));
    }

    private static (DateTimeOffset Anchor, string Text) Split(string input)
    {
        var parts = input.Split(separator: '|');

        return (DateTimeOffset.Parse(parts[0]), parts[1]);
    }
}
