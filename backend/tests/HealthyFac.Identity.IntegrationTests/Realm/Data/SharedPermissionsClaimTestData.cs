namespace HealthyFac.Identity.IntegrationTests.Realm.Data;

public static class SharedPermissionsClaimTestData
{
    public const string SecondApiClientId = "test-second-api";
    public const string SecondApiPermission = "widgets:read";

    public static IReadOnlyList<string> ExpectedMergedPermissions { get; } =
    [
        .. RealmTestData.ExpectedPermissions,
        SecondApiPermission,
    ];
}
