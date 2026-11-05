namespace HealthyFac.Shared.Application.Grids;

/// <summary>
/// Names the one column that a grid sorts by and the direction.
/// </summary>
/// <typeparam name="TSortColumn">The enumeration of the sortable columns of the grid.</typeparam>
public sealed record GridSort<TSortColumn>
{
    public required TSortColumn Column { get; init; }

    public required SortDirection Direction { get; init; }
}
