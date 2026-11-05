using Bogus;

namespace HealthyFac.Shared.IntegrationTests.Grids.Data;

public static class GridTestData
{
    public const string SortGroup = "sort";
    public const string PagingGroup = "paging";
    public const string TextGroup = "text";
    public const string DatesGroup = "dates";

    public const int PagingRowCount = 5000;
    public const int PagedRowCount = 30;
    public const int PageSize = 10;
    public const int DatesRowCount = 10;

    public const string PrefixAb = "ab";
    public const string PercentPrefix = "50%";

    private const int Seed = 4711;
    private const int SortRowCount = 40;
    private const string Lowercase = "abcdefghijklmnopqrstuvwxyz";
    private const int TextLength = 8;

    private static readonly string[] FixedTexts = ["ABC", "abd", "xab", "50% off", "500 off", "50_x", "50ax"];

    private static readonly DateTime SortBase = new(
        year: 2026, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc);

    private static readonly DateTime PagingAt = new(
        year: 2030, month: 6, day: 1, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime DatesBase = new(
        year: 2027, month: 1, day: 1, hour: 0, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly TimeSpan DatesStep = TimeSpan.FromHours(hours: 1);

    public static TheoryData<string, bool> SortCases
    {
        get
        {
            var data = new TheoryData<string, bool>();

            foreach (var column in new[] { "text", "number", "amount", "at", "key" })
            {
                data.Add(column, p2: false);
                data.Add(column, p2: true);
            }

            return data;
        }
    }

    public static TheoryData<int?, int?> DateRangeCases => new()
    {
        { 3, 7 },
        { 3, null },
        { null, 7 },
        { null, null },
        { 5, 5 },
    };

    public static List<GridTestEntity> CreateRows()
    {
        var faker = new Faker { Random = new Randomizer(Seed) };
        var rows = new List<GridTestEntity>();

        for (var index = 0; index < SortRowCount; index++)
        {
            var at = SortBase.AddMinutes(faker.Random.Int(min: 0, max: 100_000));
            rows.Add(
                new GridTestEntity
                {
                    Group = SortGroup,
                    Text = faker.Random.String2(TextLength, Lowercase),
                    Number = faker.Random.Int(min: 0, max: 9),
                    Amount = faker.Random.Decimal(min: 0, max: 1000),
                    At = at,
                    OptionalAt = index % 3 == 0 ? null : at.AddDays(value: 1),
                    Key = faker.Random.Guid(),
                });
        }

        for (var index = 0; index < PagingRowCount; index++)
        {
            rows.Add(
                new GridTestEntity
                {
                    Group = PagingGroup,
                    Text = faker.Random.String2(TextLength, Lowercase),
                    Number = index,
                    Amount = index,
                    At = PagingAt,
                    Key = faker.Random.Guid(),
                });
        }

        rows.AddRange(
            FixedTexts.Select(text => new GridTestEntity
            {
                Group = TextGroup,
                Text = text,
                Number = 0,
                Amount = 0,
                At = SortBase,
                Key = faker.Random.Guid(),
            }));

        for (var index = 0; index < DatesRowCount; index++)
        {
            var at = DatesBase + DatesStep * index;
            rows.Add(
                new GridTestEntity
                {
                    Group = DatesGroup,
                    Text = faker.Random.String2(TextLength, Lowercase),
                    Number = index,
                    Amount = index,
                    At = at,
                    OptionalAt = index % 2 == 0 ? null : at,
                    Key = faker.Random.Guid(),
                });
        }

        return rows;
    }

    public static IComparer<Guid> GuidBytewise { get; } = Comparer<Guid>.Create((left, right) =>
        left.ToByteArray(bigEndian: true)
            .AsSpan()
            .SequenceCompareTo(right.ToByteArray(bigEndian: true)));
}
