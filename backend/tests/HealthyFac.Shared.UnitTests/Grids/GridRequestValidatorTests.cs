using HealthyFac.Shared.Application.Filters;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.UnitTests.Grids.Data;

namespace HealthyFac.Shared.UnitTests.Grids;

public sealed class GridRequestValidatorTests
{
    [Theory]
    [MemberData(nameof(GridValidatorTestData.RangesWithFromNotBeforeTo), MemberType = typeof(GridValidatorTestData))]
    public void Validate_WithFromNotBeforeTo_Fails(DateTime from, DateTime to)
    {
        var filter = new DateRangeFilter { From = from, To = to };

        var result = new DateRangeFilterValidator().Validate(filter);

        result.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(GridErrors.Validation.DateRangeFromNotBeforeTo);
    }

    [Fact]
    public void Validate_WithOpenBound_Passes()
    {
        var filter = new DateRangeFilter { From = GridValidatorTestData.RangeStart };

        var result = new DateRangeFilterValidator().Validate(filter);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithLimitAboveMax_Fails()
    {
        var validator = new TestGridRequestValidator(TestSortColumn.All);

        var result = validator.Validate(GridValidatorTestData.CreateRequest(GridValidatorTestData.AboveMaxLimit));

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(GridErrors.Validation.LimitOutOfRange);
    }

    [Fact]
    public void Validate_WithUnknownSortColumn_Fails()
    {
        var validator = new TestGridRequestValidator([TestSortColumn.Name]);
        var sort = new GridSort<TestSortColumn>
            { Column = TestSortColumn.CreatedAt, Direction = SortDirection.Ascending };

        var result = validator.Validate(GridValidatorTestData.CreateRequest(sort: sort));

        result.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(GridErrors.Validation.SortColumnUnknown);
    }

    [Fact]
    public void Validate_WithKnownSortColumnAndMaxLimit_Passes()
    {
        var validator = new TestGridRequestValidator(TestSortColumn.All);
        var sort = new GridSort<TestSortColumn> { Column = TestSortColumn.Name, Direction = SortDirection.Descending };

        var result = validator.Validate(GridValidatorTestData.CreateRequest(GridValidationConstants.MaxLimit, sort));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(GridValidatorTestData.NonUtcKinds), MemberType = typeof(GridValidatorTestData))]
    public void Validate_WithNonUtcDateBound_Fails(DateTimeKind kind)
    {
        var from = DateTime.SpecifyKind(GridValidatorTestData.RangeStart, kind);
        var to = DateTime.SpecifyKind(GridValidatorTestData.RangeEnd, kind);

        var fromResult = new DateRangeFilterValidator().Validate(new DateRangeFilter { From = from });
        var toResult = new DateRangeFilterValidator().Validate(new DateRangeFilter { To = to });

        fromResult.Errors.Should()
            .ContainSingle()
            .Which.ErrorMessage.Should()
            .Be(GridErrors.Validation.DateRangeNotUtc);
        toResult.Errors.Should().ContainSingle().Which.ErrorMessage.Should().Be(GridErrors.Validation.DateRangeNotUtc);
    }

    [Theory]
    [MemberData(nameof(GridValidatorTestData.InvalidIdLists), MemberType = typeof(GridValidatorTestData))]
    public void Validate_WithNullOrEmptyIds_Fails(IReadOnlyList<Guid>? ids)
    {
        var result = new IdSetFilterValidator().Validate(new IdSetFilter { Ids = ids! });

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithRealIds_Passes()
    {
        var result = new IdSetFilterValidator().Validate(new IdSetFilter { Ids = [Guid.NewGuid()] });

        result.IsValid.Should().BeTrue();
    }
}
