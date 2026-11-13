namespace HealthyFac.Shared.UnitTests.Configuration.Data;

public static class DeploymentSettingsExtensionsTestData
{
    public const string ConfigurationKey = "HfDeploymentTest:Value";
    public const string EnvironmentVariableName = "HfDeploymentTest__Value";
    public const string HostVariablePrefix = "ASPNETCORE_";
    public const string EarlierSourceValue = "from-appsettings";
    public const string FileValue = "from-deployment-file";
    public const string EnvironmentValue = "from-environment";

    public const string FileContent = """
                                      { "HfDeploymentTest": { "Value": "from-deployment-file" } }
                                      """;
}
