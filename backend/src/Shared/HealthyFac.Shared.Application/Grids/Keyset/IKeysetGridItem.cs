namespace HealthyFac.Shared.Application.Grids.Keyset;

/// <summary>
/// Marks a grid row that carries its keyset position.
/// </summary>
/// <typeparam name="TSelf">The row type itself.</typeparam>
public interface IKeysetGridItem<TSelf>
    where TSelf : IKeysetGridItem<TSelf>
{
    /// <summary>The position of the row under the sort of the request.</summary>
    KeysetPosition? Position { get; }

    /// <param name="position">The position to set.</param>
    /// <returns>A copy of the row with the position.</returns>
    TSelf WithPosition(KeysetPosition position);
}
