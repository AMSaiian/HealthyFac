using HealthyFac.Shared.Infrastructure.Seeding;
using Microsoft.Extensions.Time.Testing;

namespace HealthyFac.Shared.UnitTests.Seeding;

public sealed class SeedTimeShiftTests
{
    private static readonly DateTimeOffset ReferenceTime = new(2026, 11, 10, 13, 44, 48, TimeSpan.Zero);
    private static readonly DateTimeOffset SeedTime = new(2026, 12, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly SeedTimeShift _shift = SeedTimeShift.Create(ReferenceTime, new FakeTimeProvider(SeedTime));

    [Fact]
    public void Shift_WithTheReferenceTime_ReturnsTheSeedTimeMinusOneHour()
    {
        var shifted = _shift.Shift(ReferenceTime.UtcDateTime);

        shifted.Should().Be(SeedTime.UtcDateTime - TimeSpan.FromHours(hours: 1));
        shifted!.Value.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public void Shift_WithTwoValues_KeepsTheGapBetweenThem()
    {
        var first = ReferenceTime.UtcDateTime - TimeSpan.FromSeconds(seconds: 73);
        var second = ReferenceTime.UtcDateTime - TimeSpan.FromSeconds(seconds: 25);

        var gap = _shift.Shift(second) - _shift.Shift(first);

        gap.Should().Be(second - first);
    }

    [Fact]
    public void Shift_WithNull_ReturnsNull()
    {
        var shifted = _shift.Shift(value: null);

        shifted.Should().BeNull();
    }

    [Fact]
    public void Shift_WithALocalValue_ShiftsItsUtcMoment()
    {
        var local = ReferenceTime.UtcDateTime.ToLocalTime();

        var shifted = _shift.Shift(local);

        shifted.Should().Be(_shift.Shift(ReferenceTime.UtcDateTime));
    }
}
