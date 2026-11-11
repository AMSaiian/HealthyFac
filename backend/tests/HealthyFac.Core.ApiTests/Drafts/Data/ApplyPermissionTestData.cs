namespace HealthyFac.Core.ApiTests.Drafts.Data;

public static class ApplyPermissionTestData
{
    public static TheoryData<string> MissingOnePermission => new()
    {
        DraftApiTestData.DraftWritePermission,
        DraftApiTestData.MachineWritePermission,
    };
}
