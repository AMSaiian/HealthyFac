using System.Collections.Frozen;
using HealthyFac.Shared.Application.Grids.Keyset;

namespace HealthyFac.Shared.UnitTests.Grids.Keyset.Data;

public static class KeysetSortMapTestData
{
    public static TheoryData<string, Type, int> Cases => new()
    {
        { "map with a nullable key", typeof(UnsupportedKeyMap), 1 },
        { "map with int and DateTime keys", typeof(SupportedKeyMap), 0 },
    };

    public static class UnsupportedKeyMap
    {
        public static readonly FrozenDictionary<string, IKeysetSortKey<KeysetSortKeyTestRow>> Instance =
            new Dictionary<string, IKeysetSortKey<KeysetSortKeyTestRow>>
            {
                ["number"] = KeysetSortKey.For((KeysetSortKeyTestRow row) => row.NullableNumber),
            }.ToFrozenDictionary();
    }

    public static class SupportedKeyMap
    {
        public static readonly FrozenDictionary<string, IKeysetSortKey<KeysetSortKeyTestRow>> Instance =
            new Dictionary<string, IKeysetSortKey<KeysetSortKeyTestRow>>
            {
                ["number"] = KeysetSortKey.For((KeysetSortKeyTestRow row) => row.Number),
                ["at"] = KeysetSortKey.For((KeysetSortKeyTestRow row) => row.At),
            }.ToFrozenDictionary();
    }
}
