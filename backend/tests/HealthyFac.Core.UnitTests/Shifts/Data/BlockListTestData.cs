using System.Globalization;
using HealthyFac.Core.Application.Shifts;

namespace HealthyFac.Core.UnitTests.Shifts.Data;

public static class BlockListTestData
{
    private const string TimeFormat = "HH:mm";

    public static IReadOnlyList<ShiftBlockInput> Blocks(string[] ranges) =>
        ranges.Select(range =>
            {
                var parts = range.Split(separator: '-');

                return new ShiftBlockInput
                {
                    Start = TimeOnly.ParseExact(parts[0], TimeFormat, CultureInfo.InvariantCulture),
                    End = TimeOnly.ParseExact(parts[1], TimeFormat, CultureInfo.InvariantCulture),
                };
            })
            .ToList();

    public static TheoryData<string[]> InvalidLists => new()
    {
        new[] { "08:00-12:00", "11:00-15:00" },
        new[] { "08:00-12:03" },
        new[] { "08:02-12:00" },
        new[] { "12:00-08:00" },
        new[] { "08:00-08:00" },
        new[]
        {
            "01:00-02:00", "03:00-04:00", "05:00-06:00", "07:00-08:00", "09:00-10:00", "11:00-12:00", "13:00-14:00",
        },
        new[] { "13:00-17:00", "08:00-12:00" },
    };

    public static TheoryData<string[]> ValidLists => new()
    {
        new[] { "08:00-12:00", "12:00-16:00" },
        new[] { "08:00-12:00", "13:00-17:00" },
        new string[] { },
        new[] { "01:00-02:00", "03:00-04:00", "05:00-06:00", "07:00-08:00", "09:00-10:00", "11:00-12:00" },
    };
}
