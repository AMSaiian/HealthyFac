using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.UnitTests.Grids.Data;

namespace HealthyFac.Shared.UnitTests.Grids.Keyset.Data;

public sealed record TestKeysetGridRequest : KeysetGridRequest<TestFilter, TestSortColumn>;

public sealed class TestKeysetGridRequestValidator(IEnumerable<TestSortColumn> sortableColumns)
    : KeysetGridRequestValidator<TestKeysetGridRequest, TestFilter, TestSortColumn>(sortableColumns);

public static class KeysetGridRequestTestData
{
    public static TestKeysetGridRequest CreateRequestWithoutFilter() =>
        new()
        {
            Limit = GridValidationConstants.MinLimit,
            Filter = null,
        };
}
