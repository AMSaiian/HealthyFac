namespace HealthyFac.Core.ApiTests.Drafts.Data;

public static class ApplyPermissionTestData
{
    public static TheoryData<string> MissingOnePermission =>
    [
        DraftApiTestData.DraftWritePermission,
        DraftApiTestData.MachineWritePermission,
    ];
}
