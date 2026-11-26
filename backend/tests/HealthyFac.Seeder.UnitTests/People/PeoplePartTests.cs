using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.UnitTests.People.Data;

namespace HealthyFac.Seeder.UnitTests.People;

public sealed class PeoplePartTests
{
    [Theory(Explicit = true)]
    [MemberData(nameof(PeoplePartTestData.InvalidAcceptMoments), MemberType = typeof(PeoplePartTestData))]
    public void ReadEvents_WithAnAcceptMomentThatCannotCarryTheShiftEvent_FailsWithTheItem(
        string name,
        string acceptedAt,
        string reason
    )
    {
        var folder = CreateFolder(acceptedAt);
        var part = new PeoplePart(null!, null!);

        var act = () => part.ReadEvents(folder);

        act.Should().Throw<SeedFileException>(name).WithMessage($"*{PeoplePartTestData.Item}*{reason}*");
    }

    [Fact(Explicit = true)]
    public void ReadEvents_WithAValidAcceptMoment_ExposesTheCompanyZoneOfTheFile()
    {
        var folder = CreateFolder(acceptedAt: "d-1 09:00");
        var part = new PeoplePart(null!, null!);

        part.ReadEvents(folder);

        part.CompanyZoneId.Should().Be(expected: "Europe/Kyiv");
    }

    private static SeedFolder CreateFolder(string acceptedAt)
    {
        var directory = Directory.CreateTempSubdirectory().FullName;

        File.WriteAllText(
            Path.Combine(directory, PeoplePartTestData.FileName), PeoplePartTestData.CompanyJson(acceptedAt));

        return new SeedFolder(directory);
    }
}
