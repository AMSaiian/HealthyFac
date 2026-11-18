using System.Collections.Frozen;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.Grids.Keyset;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Domain.Enumerations;

namespace HealthyFac.Shared.IntegrationTests.Grids.Data;

public sealed class KeysetSortColumn : Enumeration<KeysetSortColumn>
{
    public static readonly KeysetSortColumn Rank = new(value: "rank");
    public static readonly KeysetSortColumn Big = new(value: "big");
    public static readonly KeysetSortColumn Amount = new(value: "amount");
    public static readonly KeysetSortColumn Ratio = new(value: "ratio");
    public static readonly KeysetSortColumn At = new(value: "at");
    public static readonly KeysetSortColumn Name = new(value: "name");

    private KeysetSortColumn(string value)
        : base(value)
    {
    }
}

public sealed record KeysetTestItem : IKeysetGridItem<KeysetTestItem>, IEntity
{
    public required Guid Id { get; init; }

    public required int Rank { get; init; }

    public required long Big { get; init; }

    public required decimal Amount { get; init; }

    public required double Ratio { get; init; }

    public required DateTime At { get; init; }

    public required string Name { get; init; }

    public KeysetPosition? Position { get; init; }

    public KeysetTestItem WithPosition(KeysetPosition position) => this with { Position = position };
}

public static class KeysetTestSortMap
{
    public static readonly FrozenDictionary<KeysetSortColumn, IKeysetSortKey<KeysetTestItem>> Instance =
        new Dictionary<KeysetSortColumn, IKeysetSortKey<KeysetTestItem>>
        {
            [KeysetSortColumn.Rank] = KeysetSortKey.For((KeysetTestItem item) => item.Rank),
            [KeysetSortColumn.Big] = KeysetSortKey.For((KeysetTestItem item) => item.Big),
            [KeysetSortColumn.Amount] = KeysetSortKey.For((KeysetTestItem item) => item.Amount),
            [KeysetSortColumn.Ratio] = KeysetSortKey.For((KeysetTestItem item) => item.Ratio),
            [KeysetSortColumn.At] = KeysetSortKey.For((KeysetTestItem item) => item.At),
            [KeysetSortColumn.Name] = KeysetSortKey.For((KeysetTestItem item) => item.Name),
        }.ToFrozenDictionary();
}

public static class KeysetTestData
{
    public const int PageLimit = 7;

    private const int RowCount = 25;
    private const long BigBase = 10_000_000_000;

    private static readonly string[] Names = ["Pump", "pump", "Valve", "valve", "Motor"];

    private static readonly DateTime AtBase = new(
        year: 2028, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc);

    public static TheoryData<string, bool> Cases
    {
        get
        {
            var data = new TheoryData<string, bool>();

            foreach (var column in new[] { "rank", "big", "amount", "ratio", "at", "name" })
            {
                data.Add(column, p2: false);
                data.Add(column, p2: true);
            }

            return data;
        }
    }

    public static TheoryData<string, string> InvalidPositionCases => new()
    {
        { "rank", "\"abc\"" },
        { "rank", "null" },
        { "at", "\"not-a-date\"" },
        { "at", "5" },
        { "at", "\"2028-01-01T00:00:00\"" },
        { "at", "\"2028-01-01T00:00:00+02:00\"" },
        { "name", "5" },
        { "big", "1.5" },
    };

    public static List<GridTestEntity> CreateRows() =>
        Enumerable
            .Range(start: 0, RowCount)
            .Select(index => new GridTestEntity
            {
                Group = GridTestData.KeysetGroup,
                Text = Names[index % Names.Length],
                Number = index % 5,
                Amount = index % 6 * 1.5m,
                At = AtBase.AddHours(index % 7),
                Key = Guid.CreateVersion7(),
                Big = BigBase + index % 4,
                Ratio = index % 3 * 0.25,
            })
            .ToList();

    public static IEnumerable<KeysetTestItem> Order(
        IEnumerable<KeysetTestItem> items,
        string columnName,
        bool isDescending
    )
    {
        Func<KeysetTestItem, object> key = columnName switch
        {
            "rank" => item => item.Rank,
            "big" => item => item.Big,
            "amount" => item => item.Amount,
            "ratio" => item => item.Ratio,
            "at" => item => item.At,
            _ => item => item.Name,
        };
        IComparer<object> comparer = columnName == "name"
            ? Comparer<object>.Create((left, right) =>
                StringComparer.OrdinalIgnoreCase.Compare((string)left, (string)right))
            : Comparer<object>.Default;

        return isDescending
            ? items.OrderByDescending(key, comparer).ThenByDescending(item => item.Id, GridTestData.GuidBytewise)
            : items.OrderBy(key, comparer).ThenBy(item => item.Id, GridTestData.GuidBytewise);
    }

    public static GridSort<KeysetSortColumn> SortOf(string columnName, bool isDescending) =>
        new()
        {
            Column = KeysetSortColumn.FromValue(columnName),
            Direction = isDescending ? SortDirection.Descending : SortDirection.Ascending,
        };
}
