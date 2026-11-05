using System.Linq.Expressions;
using HealthyFac.Shared.Application.Filters;

namespace HealthyFac.Shared.UnitTests.Grids.Filters;

public sealed class PredicatesTests
{
    private static readonly int[] Values = [1, 3, 6];

    private static readonly Expression<Func<int, bool>> IsOne = value => value == 1;

    private static readonly Expression<Func<int, bool>> IsAboveOne = left => left > 1;

    private static readonly Expression<Func<int, bool>> IsBelowFive = right => right < 5;

    private static readonly Expression<Func<int, bool>> IsAboveFive = other => other > 5;

    [Fact]
    public void And_WithBothSidesNull_ReturnsNull()
    {
        var predicate = Predicates.From<int>(null).And(null);

        predicate.Should().BeNull();
    }

    [Fact]
    public void And_WithNullLeft_ReturnsRight()
    {
        var predicate = Predicates.From<int>(null).And(IsBelowFive);

        predicate.Should().BeSameAs(IsBelowFive);
    }

    [Fact]
    public void And_WithPredicatesOfDifferentParameters_MatchesValuesThatHoldBoth()
    {
        var predicate = Predicates.From(IsAboveOne).And(IsBelowFive);

        var matches = Values.Where(predicate!.Compile()).ToList();

        matches.Should().Equal(3);
    }

    [Fact]
    public void Or_WithNullRight_ReturnsLeft()
    {
        var predicate = Predicates.From(IsAboveOne).Or(null);

        predicate.Should().BeSameAs(IsAboveOne);
    }

    [Fact]
    public void Or_WithPredicatesOfDifferentParameters_MatchesValuesThatHoldEither()
    {
        var predicate = Predicates.From(IsOne).Or(IsAboveFive);

        var matches = Values.Where(predicate!.Compile()).ToList();

        matches.Should().Equal(1, 6);
    }
}
