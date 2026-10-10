using System.Globalization;
using HealthyFac.Shared.Infrastructure.Persistence.CheckConstraints;
using HealthyFac.Shared.UnitTests.Persistence.Data;

namespace HealthyFac.Shared.UnitTests.Persistence;

public sealed class CheckConstraintSqlTests
{
    [Theory]
    [MemberData(nameof(CheckConstraintTestData.RangeExpressions), MemberType = typeof(CheckConstraintTestData))]
    public void Range_WithDecimalBoundsUnderCommaCulture_UsesInvariantSeparator(
        string column,
        double min,
        double max,
        string expected
    )
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");

        try
        {
            CheckConstraintSql.Range(column, min, max).Should().Be(expected);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [MemberData(nameof(CheckConstraintTestData.AllowedValueExpressions), MemberType = typeof(CheckConstraintTestData))]
    public void AllowedValues_WithValues_SortsAndEscapesQuotes(string column, string[] values, string expected) =>
        CheckConstraintSql.AllowedValues(column, values).Should().Be(expected);

    [Fact]
    public void ConstraintName_WhenOverIdentifierLimit_Throws()
    {
        var act = () => CheckConstraintSql.ConstraintName(CheckConstraintTestData.LongEntity, "name", "not_blank");

        act.Should().Throw<ArgumentException>();
    }
}
