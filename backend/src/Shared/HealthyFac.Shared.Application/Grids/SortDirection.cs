using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.Application.Grids;

public sealed class SortDirection : Enumeration<SortDirection>
{
    public static readonly SortDirection Ascending = new(value: "asc");
    public static readonly SortDirection Descending = new(value: "desc");

    private SortDirection(string value)
        : base(value)
    {
    }
}
