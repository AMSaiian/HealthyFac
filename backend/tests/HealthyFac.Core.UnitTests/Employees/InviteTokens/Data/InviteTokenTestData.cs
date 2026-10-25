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

    public static Guid InviteId { get; } = Guid.Parse("0d4f4b6e-3c1a-4e57-9a5e-6f1b2c3d4e5f");

    public static TheoryData<string> MalformedTokens => new()
    {
        string.Empty,
        "abc",
        "a.b",
        "not-a-guid.k1.AAAA",
        $"{InviteId:N}.k1.%%%",
        $"{InviteId:N}.k1.sig.extra",
    };

    public static TheoryData<string> TokensWithUnreadableInviteId => new()
    {
        string.Empty,
        "abc",
        "a.b",
        "not-a-guid.k1.AAAA",
        $"{InviteId:N}.k1.sig.extra",
    };

    private static readonly string ValidSecret = Convert.ToBase64String(new byte[KeyByteCount]);

    public static TheoryData<InviteTokenOptions> InvalidOptions => new()
    {
        BuildOptions(keys: []),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: true), BuildKey(OlderKeyId, isDefault: true)]),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: false)]),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: true), BuildKey(CurrentKeyId, isDefault: false)]),
        BuildOptions(keys: [BuildKey("k.1", isDefault: true)]),
        BuildOptions(keys: [BuildKey(CurrentKeyId, isDefault: true, secret: "not base64 !!")]),
        BuildOptions(
            keys:
            [
                BuildKey(
                    CurrentKeyId,
                    isDefault: true,
                    secret: Convert.ToBase64String(new byte[TooShortKeyByteCount])),
            ]),
        BuildOptions(keys: [new InviteTokenKeyOptions { Id = null!, Secret = ValidSecret, IsDefault = true }]),
        BuildOptions(keys: [new InviteTokenKeyOptions { Id = CurrentKeyId, Secret = null!, IsDefault = true }]),
        BuildOptions(lifetimeHours: 0),
        BuildOptions(lifetimeHours: TooLongLifetimeHours),
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
                BuildKey(CurrentKeyId, isDefault: defaultKeyId == CurrentKeyId),
                BuildKey(OlderKeyId, isDefault: defaultKeyId == OlderKeyId),
            ]);

    private static InviteTokenKeyOptions BuildKey(string id, bool isDefault, string? secret = null) =>
        new()
        {
            Id = id,
            Secret = secret ?? Convert.ToBase64String(Enumerable.Repeat((byte)id[^1], KeyByteCount).ToArray()),
            IsDefault = isDefault,
        };
}
