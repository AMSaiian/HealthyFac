using System.Text.Json.Nodes;
using HealthyFac.Seeder.Cases;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.UnitTests.Cases.Data;
using HealthyFac.Seeder.UnitTests.SeedFiles.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.UnitTests.Cases;

[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Cases)]
public sealed class CasesPartTests
{
    [Fact(Explicit = true)]
    public void ReadEvents_WithACoverageOtherThanDrafted_FailsWithTheItem()
    {
        var folder = CreateFolder(signal => signal["coverage"] = "watched");
        var part = new CasesPart(null!);

        var act = () => part.ReadEvents(folder);

        act.Should().Throw<SeedFileException>().WithMessage($"*{CasesPartTestData.Item}*watched*only drafted*");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAMetricKeyThatTheMachineDoesNotHave_FailsWithTheFileTheItemAndTheRule()
    {
        var folder = CreateFolder(signal => signal["metricKey"] = CasesPartTestData.UnknownMetricKey);
        var part = new CasesPart(null!);

        var act = () => part.ReadEvents(folder);

        act.Should()
            .Throw<SeedFileException>()
            .WithMessage($"*{CasesPartTestData.Item}*{CasesPartTestData.UnknownMetricKey}*does not have*");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithTheDraftedCoverage_ReturnsTheCaseEvent()
    {
        var folder = CreateFolder(_ => { });
        var part = new CasesPart(null!);

        var events = part.ReadEvents(folder);

        events.Should().ContainSingle().Which.Item.Should().Be(CasesPartTestData.Item);
    }

    private static SeedFolder CreateFolder(Action<JsonNode> changeSignal) =>
        CommittedSeedTestData.CopyWith(
            CasesPartTestData.FileName,
            root => changeSignal(root["signal"]!));
}
