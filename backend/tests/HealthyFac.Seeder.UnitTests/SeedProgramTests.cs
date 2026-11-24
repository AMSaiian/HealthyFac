using HealthyFac.Seeder.Cases;
using HealthyFac.Seeder.Ingest;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.TelemetryHistory;
using HealthyFac.Seeder.WorkOrders;

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

    [Theory(Explicit = true)]
    [MemberData(nameof(SeedProgramTestData.KnownArguments), MemberType = typeof(SeedProgramTestData))]
    public async Task RunAsync_WithAKnownArgument_FailsInTheSetupAndNotOnTheArgument(string[] args)
    {
        var output = new StringWriter();
        var emptyDirectory = Directory.CreateTempSubdirectory().FullName;

        var exitCode = await SeedProgram.RunAsync(args, output, emptyDirectory, CancellationToken.None);

        exitCode.Should().Be(expected: 1);
        output.ToString().Should().Contain(expected: "appsettings.json").And.NotContain(unexpected: "is not known");
    }

    [Fact(Explicit = true)]
    public void Create_InModeTwo_ListsTheTelemetryPartBeforeTheMachinesPart()
    {
        var parts = SeedParts.Create(null!, new PeoplePart(null!, null!), isTelemetry: true);

        parts.Select(part => part.GetType())
            .Should()
            .Equal(
                typeof(PeoplePart),
                typeof(IngestPart),
                typeof(TelemetryHistoryPart),
                typeof(MachinesPart),
                typeof(CasesPart),
                typeof(WorkOrdersPart));
    }

    [Fact(Explicit = true)]
    public void Create_InModeOne_ListsNoTelemetryPart()
    {
        var parts = SeedParts.Create(null!, new PeoplePart(null!, null!), isTelemetry: false);

        parts.Should().NotContain(part => part is TelemetryHistoryPart);
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
