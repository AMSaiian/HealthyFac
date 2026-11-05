using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.UnitTests.Grids.Data;

public sealed class TestSortColumn : Enumeration<TestSortColumn>
{
    public static readonly TestSortColumn Name = new("name");
    public static readonly TestSortColumn CreatedAt = new("createdAt");

    private TestSortColumn(string value)
        : base(value)
    {
    }
}

public sealed record TestFilter;

public sealed record TestGridRequest : GridRequest<TestFilter, TestSortColumn>;

public sealed class TestGridRequestValidator(IEnumerable<TestSortColumn> sortableColumns)
    : GridRequestValidator<TestGridRequest, TestFilter, TestSortColumn>(sortableColumns);

public static class GridValidatorTestData
{
    public const int AboveMaxLimit = GridValidationConstants.MaxLimit + 1;

    public static readonly DateTime RangeStart = new(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);

    public static readonly DateTime RangeEnd = RangeStart.AddDays(1);

    public static TheoryData<DateTimeKind> NonUtcKinds => new()
    {
        DateTimeKind.Unspecified,
        DateTimeKind.Local,
    };

    public static TheoryData<IReadOnlyList<Guid>?> InvalidIdLists => new()
    {
        null,
        new List<Guid> { Guid.Empty },
        new List<Guid> { Guid.NewGuid(), Guid.Empty },
    };

    public static TestGridRequest CreateRequest(
        int limit = GridValidationConstants.MinLimit,
        GridSort<TestSortColumn>? sort = null
    ) =>
        new()
        {
            Offset = GridValidationConstants.MinOffset,
            Limit = limit,
            Sort = sort,
            Filter = new TestFilter(),
        };
}
