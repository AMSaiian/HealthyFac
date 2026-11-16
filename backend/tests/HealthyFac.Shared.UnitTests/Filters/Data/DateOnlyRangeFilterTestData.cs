using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Shared.UnitTests.Filters.Data;

public static class DateOnlyRangeFilterTestData
{
    public static readonly DateOnly Day1 = new(year: 2099, month: 1, day: 1);
    public static readonly DateOnly Day2 = new(year: 2099, month: 1, day: 2);
    public static readonly DateOnly Day3 = new(year: 2099, month: 1, day: 3);
    public static readonly DateOnly Day4 = new(year: 2099, month: 1, day: 4);

    public static readonly DateOnly[] Days = [Day1, Day2, Day3, Day4];

    public static TheoryData<string, DateOnlyRangeFilter, DateOnly[]> PredicateCases =>
        new()
        {
            { "OnlyFrom", new DateOnlyRangeFilter { From = Day2 }, [Day2, Day3, Day4] },
            { "OnlyTo", new DateOnlyRangeFilter { To = Day3 }, [Day1, Day2] },
            { "Both", new DateOnlyRangeFilter { From = Day2, To = Day4 }, [Day2, Day3] },
            { "FromEqualsDayIsIncluded", new DateOnlyRangeFilter { From = Day4, To = Day4.AddDays(value: 1) }, [Day4] },
            { "ToEqualsDayIsExcluded", new DateOnlyRangeFilter { From = Day1, To = Day1 }, [] },
        };

    public static TheoryData<string, DateOnlyRangeFilter, bool> ValidatorCases =>
        new()
        {
            { "FromEqualsTo", new DateOnlyRangeFilter { From = Day2, To = Day2 }, false },
            { "FromAfterTo", new DateOnlyRangeFilter { From = Day3, To = Day2 }, false },
            { "FromOneDayBeforeTo", new DateOnlyRangeFilter { From = Day2, To = Day3 }, true },
            { "OnlyFrom", new DateOnlyRangeFilter { From = Day2 }, true },
        };
}
