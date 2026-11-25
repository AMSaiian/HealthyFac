namespace HealthyFac.Document.IntegrationTests.Files.Data;

public static class LookupFilesTestData
{
    public static TheoryData<string, IReadOnlyList<string>?, string?, bool> IdsAndSearchCombinations => new()
    {
        { "IdsOnly", ["id"], null, true },
        { "SearchOnly", null, "manual", true },
        { "Both", ["id"], "manual", false },
        { "Neither", null, null, false },
    };
}
