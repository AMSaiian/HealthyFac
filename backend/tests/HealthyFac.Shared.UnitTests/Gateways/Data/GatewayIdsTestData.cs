using HealthyFac.Shared.Domain.Gateways;

namespace HealthyFac.Shared.UnitTests.Gateways.Data;

public static class GatewayIdsTestData
{
    public static TheoryData<string?> ValidIds =>
        new()
        {
            { "abcdefghijklmnop" },
            { "qrstuvwxyz234567" },
            { "aaaaaaaaaaaaaaaa" },
            { "7777777777777777" },
        };

    public static TheoryData<string?> InvalidIds =>
        new()
        {
            { (string?)null },
            { string.Empty },
            { "abcdefghijklmno" },
            { "abcdefghijklmnopq" },
            { "ABCDEFGHIJKLMNOP" },
            { "abcdefghijklmno0" },
            { "abcdefghijklmno1" },
            { "abcdefghijklmno8" },
            { "abcdefghijklmno9" },
            { "abcdefghijklmno-" },
            { "abcdefghijklmno " },
        };

    public static int CreatedCount => 500;

    public static int ExpectedLength => GatewayIds.Length;
}
