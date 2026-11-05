using HealthyFac.Core.Infrastructure.Employees.InviteTokens;

namespace HealthyFac.Core.UnitTests.Employees.InviteTokens.Data;

public static class InviteTokenTestData
{
    public const string CurrentKeyId = "k1";
    public const string OlderKeyId = "k0";
    public const int LifetimeHours = 72;

    private const int KeyByteCount = 32;
    private const int TooShortKeyByteCount = 16;
    private const int TooLongLifetimeHours = 721;

    public static Guid InviteId { get; } = Guid.Parse(input: "0d4f4b6e-3c1a-4e57-9a5e-6f1b2c3d4e5f");

    public static TheoryData<string> MalformedTokens => new()
    {
        "a.b",
        $"{InviteId:N}.k1.%%%",
    };

    public static TheoryData<string> TokensWithUnreadableInviteId => new()
    {
        "a.b",
        "not-a-guid.k1.AAAA",
        $"{InviteId:N}.k1.sig.extra",
    };

    public static TheoryData<InviteTokenOptions> InvalidOptions => new()
    {
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: true), BuildKey(OlderKeyId, isDefault: true)]),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: false)]),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: true), BuildKey(CurrentKeyId, isDefault: false)]),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: true, secret: "not base64 !!")]),
        BuildOptions(
            keys:
            [
                BuildKey(
                    CurrentKeyId,
                    isDefault: true,
                    Convert.ToBase64String(new byte[TooShortKeyByteCount])),
            ]),
        BuildOptions(lifetimeHours: 0),
        BuildOptions(TooLongLifetimeHours),
    };

    public static InviteTokenOptions BuildOptions(
        int lifetimeHours = LifetimeHours,
        List<InviteTokenKeyOptions>? keys = null
    ) =>
        new()
        {
            LifetimeHours = lifetimeHours,
            Keys = keys ?? [BuildKey(CurrentKeyId, isDefault: true)],
        };

    public static InviteTokenOptions BuildTwoKeyOptions(string defaultKeyId) =>
        BuildOptions(
            keys:
            [
                BuildKey(CurrentKeyId, defaultKeyId == CurrentKeyId),
                BuildKey(OlderKeyId, defaultKeyId == OlderKeyId),
            ]);

    private static InviteTokenKeyOptions BuildKey(string id, bool isDefault, string? secret = null) =>
        new()
        {
            Id = id,
            Secret = secret ?? Convert.ToBase64String(Enumerable.Repeat((byte)id[^1], KeyByteCount).ToArray()),
            IsDefault = isDefault,
        };
}
