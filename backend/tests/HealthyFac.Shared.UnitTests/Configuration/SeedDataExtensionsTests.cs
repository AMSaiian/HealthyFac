using HealthyFac.Shared.UnitTests.Configuration.Data;
using HealthyFac.Shared.Web.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;

namespace HealthyFac.Shared.UnitTests.Configuration;

public sealed class SeedDataExtensionsTests : IDisposable
{
    private readonly string _baseDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(format: "N"));

    public SeedDataExtensionsTests()
    {
        Directory.CreateDirectory(Path.Combine(_baseDirectory, SeedDataExtensionsTestData.SeedDirectoryName));
        File.WriteAllText(
            Path.Combine(
                _baseDirectory,
                SeedDataExtensionsTestData.SeedDirectoryName,
                SeedDataExtensionsTestData.SeedFileName),
            SeedDataExtensionsTestData.SeedFileContent);
    }

    [Fact]
    public void AddSeedDataFile_InDevelopment_ReadsFileValue()
    {
        var configuration = BuildConfiguration(Environments.Development);

        configuration[SeedDataExtensionsTestData.ConfigurationKey].Should().Be(SeedDataExtensionsTestData.FileValue);
    }

    [Fact]
    public void AddSeedDataFile_WithEnvironmentVariable_EnvironmentVariableWins()
    {
        Environment.SetEnvironmentVariable(
            SeedDataExtensionsTestData.EnvironmentVariableName,
            SeedDataExtensionsTestData.EnvironmentValue);

        try
        {
            var configuration = BuildConfiguration(Environments.Development);

            configuration[SeedDataExtensionsTestData.ConfigurationKey]
                .Should()
                .Be(SeedDataExtensionsTestData.EnvironmentValue);
        }
        finally
        {
            Environment.SetEnvironmentVariable(SeedDataExtensionsTestData.EnvironmentVariableName, value: null);
        }
    }

    [Fact]
    public void AddSeedDataFile_OutsideDevelopment_ReadsNoFile()
    {
        var configuration = BuildConfiguration(Environments.Production);

        configuration[SeedDataExtensionsTestData.ConfigurationKey].Should().BeNull();
    }

    public void Dispose() => Directory.Delete(_baseDirectory, recursive: true);

    private IConfigurationRoot BuildConfiguration(string environmentName) =>
        new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddSeedDataFile(new HostingEnvironment { EnvironmentName = environmentName }, _baseDirectory)
            .Build();
}
