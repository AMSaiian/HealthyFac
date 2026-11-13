using HealthyFac.Shared.UnitTests.Configuration.Data;
using HealthyFac.Shared.Web.Configuration;
using Microsoft.Extensions.Configuration;

namespace HealthyFac.Shared.UnitTests.Configuration;

public sealed class DeploymentSettingsExtensionsTests : IDisposable
{
    private readonly string _baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(format: "N"));

    public DeploymentSettingsExtensionsTests()
    {
        Directory.CreateDirectory(_baseDirectory);
    }

    [Fact]
    public void AddDeploymentSettingsFile_WithEarlierSource_FileValueWins()
    {
        WriteDeploymentFile();

        var configuration = BuildConfiguration();

        configuration[DeploymentSettingsExtensionsTestData.ConfigurationKey]
            .Should()
            .Be(DeploymentSettingsExtensionsTestData.FileValue);
    }

    [Fact]
    public void AddDeploymentSettingsFile_WithEnvironmentVariable_EnvironmentVariableWins()
    {
        WriteDeploymentFile();
        Environment.SetEnvironmentVariable(
            DeploymentSettingsExtensionsTestData.EnvironmentVariableName,
            DeploymentSettingsExtensionsTestData.EnvironmentValue);

        try
        {
            var configuration = BuildConfiguration();

            configuration[DeploymentSettingsExtensionsTestData.ConfigurationKey]
                .Should()
                .Be(DeploymentSettingsExtensionsTestData.EnvironmentValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(
                DeploymentSettingsExtensionsTestData.EnvironmentVariableName,
                value: null);
        }
    }

    [Fact]
    public void AddDeploymentSettingsFile_WithoutFile_KeepsEarlierSourceValue()
    {
        var configuration = BuildConfiguration();

        configuration[DeploymentSettingsExtensionsTestData.ConfigurationKey]
            .Should()
            .Be(DeploymentSettingsExtensionsTestData.EarlierSourceValue);
    }

    public void Dispose() => Directory.Delete(_baseDirectory, recursive: true);

    private void WriteDeploymentFile() =>
        File.WriteAllText(
            Path.Combine(_baseDirectory, DeploymentSettingsExtensions.FileName),
            DeploymentSettingsExtensionsTestData.FileContent);

    /// <summary>
    /// Builds the sources in the order of the host builder: its prefixed environment variables first, then the
    /// appsettings files (the in-memory source stands for them), then the environment variables without a prefix.
    /// </summary>
    private IConfigurationRoot BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddEnvironmentVariables(DeploymentSettingsExtensionsTestData.HostVariablePrefix)
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    [DeploymentSettingsExtensionsTestData.ConfigurationKey] =
                        DeploymentSettingsExtensionsTestData.EarlierSourceValue,
                })
            .AddEnvironmentVariables()
            .AddDeploymentSettingsFile(_baseDirectory)
            .Build();
}
