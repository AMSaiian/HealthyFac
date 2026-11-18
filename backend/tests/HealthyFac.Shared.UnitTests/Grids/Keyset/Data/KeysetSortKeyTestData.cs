using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Domain.Entities;

namespace HealthyFac.Shared.UnitTests.Grids.Keyset.Data;

public sealed record KeysetSortKeyTestRow : IEntity
{
    public required Guid Id { get; init; }

    public required int Number { get; init; }

    public required long Big { get; init; }

    public required decimal Amount { get; init; }

    public required double Ratio { get; init; }

    public required DateTime At { get; init; }

    public required string Name { get; init; }

    public int? NullableNumber { get; init; }

    public Guid Key { get; init; }

    public bool Flag { get; init; }
}

public static class KeysetSortKeyTestData
{
    public static TheoryData<string, Func<IKeysetSortKey<KeysetSortKeyTestRow>>> UnsupportedKeys => new()
    {
        { "nullable int", () => KeysetSortKey.For((KeysetSortKeyTestRow row) => row.NullableNumber) },
        { "guid", () => KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Key) },
        { "bool", () => KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Flag) },
    };

    public static IReadOnlyList<IKeysetSortKey<KeysetSortKeyTestRow>> CreateSupportedKeys() =>
    [
        KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Number),
        KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Big),
        KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Amount),
        KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Ratio),
        KeysetSortKey.For((KeysetSortKeyTestRow row) => row.At),
        KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Name),
    ];
}
