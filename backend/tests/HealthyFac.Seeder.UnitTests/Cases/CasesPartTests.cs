using HealthyFac.Seeder.Cases;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.UnitTests.Cases.Data;

namespace HealthyFac.Seeder.UnitTests.Cases;

public sealed class CasesPartTests
{
    [Fact(Explicit = true)]
    public void ReadEvents_WithACoverageOtherThanDrafted_FailsWithTheItem()
    {
        var folder = CreateFolder(coverage: "watched");
        var part = new CasesPart(null!);

        var act = () => part.ReadEvents(folder);

        act.Should().Throw<SeedFileException>().WithMessage($"*{CasesPartTestData.Item}*watched*only drafted*");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithTheDraftedCoverage_ReturnsTheCaseEvent()
    {
        var folder = CreateFolder(coverage: "drafted");
        var part = new CasesPart(null!);

        var events = part.ReadEvents(folder);

        events.Should().ContainSingle().Which.Item.Should().Be(CasesPartTestData.Item);
    }

    private static SeedFolder CreateFolder(string coverage)
    {
        var directory = Directory.CreateTempSubdirectory().FullName;
        File.WriteAllText(Path.Combine(directory, CasesPartTestData.FileName), CasesPartTestData.CasesJson(coverage));

        return new SeedFolder(directory);
    }
}
