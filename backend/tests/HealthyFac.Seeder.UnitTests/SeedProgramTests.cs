namespace HealthyFac.Seeder.UnitTests;

public sealed class SeedProgramTests
{
    [Fact(Explicit = true)]
    public async Task RunAsync_WithAnUnknownArgument_ReturnsExitCodeOneAndNamesTheArgument()
    {
        var output = new StringWriter();

        var exitCode = await SeedProgram.RunAsync(
            ["--bogus"], output, AppContext.BaseDirectory, CancellationToken.None);

        exitCode.Should().Be(expected: 1);
        output.ToString().Should().Contain(expected: "--bogus");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAFailureInTheSetup_ReturnsExitCodeOneAndPrintsTheError()
    {
        var output = new StringWriter();
        var emptyDirectory = Directory.CreateTempSubdirectory().FullName;

        var exitCode = await SeedProgram.RunAsync([], output, emptyDirectory, CancellationToken.None);

        exitCode.Should().Be(expected: 1);
        output.ToString().Should().Contain(expected: "appsettings.json");
    }
}
