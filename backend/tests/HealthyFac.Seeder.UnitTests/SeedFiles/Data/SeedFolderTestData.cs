namespace HealthyFac.Seeder.UnitTests.SeedFiles.Data;

public static class SeedFolderTestData
{
    public const string FileName = "people.json";
    public const string ValidJson = """{ "name": "anna", "kind": "user", "at": "wd-1 09:00" }""";

    public static TheoryData<string, string, string> InvalidFiles => new()
    {
        { "a missing required member", """{ "name": "anna", "kind": "user" }""", "at" },
        { "an unknown member", """{ "name": "anna", "kind": "user", "at": "d-1 10:00", "extra": 1 }""", "$.extra" },
        { "a wrong type", """{ "name": 5, "kind": "user", "at": "d-1 10:00" }""", "$.name" },
        { "an invalid moment", """{ "name": "anna", "kind": "user", "at": "wd+x 09:00" }""", "$.at" },
    };
}
