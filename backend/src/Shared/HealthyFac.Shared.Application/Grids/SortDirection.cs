using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.Grids;

public sealed class SortDirection : Enumeration<SortDirection>
{
    public static readonly SortDirection Ascending = new("asc");
    public static readonly SortDirection Descending = new("desc");

    private SortDirection(string value)
        : base(value)
    {
    }
}
