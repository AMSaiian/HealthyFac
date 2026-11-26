using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.UnitTests.Filters.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Shared.UnitTests.Filters;

[Feature(TestFeatures.Platform)]
public sealed class DateOnlyRangeFilterTests
{
    [Theory]
    [MemberData(nameof(DateOnlyRangeFilterTestData.PredicateCases), MemberType = typeof(DateOnlyRangeFilterTestData))]
    public void ToPredicate_WithBounds_KeepsOnlyDaysInHalfOpenRange(
        string caseName,
        DateOnlyRangeFilter filter,
        DateOnly[] expected
    )
    {
        var predicate = filter.ToPredicate<DateOnly>(day => day);

        var result = DateOnlyRangeFilterTestData.Days.Where(predicate!.Compile()).ToArray();

        result.Should().Equal(expected, caseName);
    }

    [Fact]
    public void ToPredicate_WithBothBoundsOpen_ReturnsNull()
    {
        var filter = new DateOnlyRangeFilter();

        var predicate = filter.ToPredicate<DateOnly>(day => day);

        predicate.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(DateOnlyRangeFilterTestData.ValidatorCases), MemberType = typeof(DateOnlyRangeFilterTestData))]
    public void Validate_WithBounds_RequiresFromBeforeTo(string caseName, DateOnlyRangeFilter filter, bool isValid)
    {
        var result = new DateOnlyRangeFilterValidator().Validate(filter);

        result.IsValid.Should().Be(isValid, caseName);
    }
}
