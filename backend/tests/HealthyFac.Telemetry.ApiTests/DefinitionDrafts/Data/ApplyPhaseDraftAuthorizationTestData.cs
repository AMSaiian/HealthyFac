namespace HealthyFac.Telemetry.ApiTests.DefinitionDrafts.Data;

public static class ApplyPhaseDraftAuthorizationTestData
{
    public static TheoryData<string, string> MissingOnePermission => new()
    {
        { "only the draft permission", "definition-drafts:write" },
        { "only the phase permission", "operating-phases:write" },
    };
}
