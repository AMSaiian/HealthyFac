namespace HealthyFac.Shared.UnitTests.Configuration.Data;

public static class SeedDataExtensionsTestData
{
    public const string ConfigurationKey = "HfSeedTest:Value";
    public const string EnvironmentVariableName = "HfSeedTest__Value";
    public const string FileValue = "from-file";
    public const string EnvironmentValue = "from-environment";
    public const string SeedDirectoryName = "seed";
    public const string SeedFileName = "seed.Development.json";

    public const string SeedFileContent = """
                                          { "HfSeedTest": { "Value": "from-file" } }
                                          """;
}
