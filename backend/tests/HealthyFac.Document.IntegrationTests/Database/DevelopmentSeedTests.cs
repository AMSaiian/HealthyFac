using HealthyFac.Document.IntegrationTests.Database.Data;

namespace HealthyFac.Document.IntegrationTests.Database;

[Collection(DocumentSeedingCollection.Name)]
public sealed class DevelopmentSeedTests(DocumentDatabaseFixture fixture)
{
    private readonly DocumentDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_CreatesRegistrations()
    {
        await _fixture.RunDevelopmentSeedAsync();

        var registrations = await _fixture.LoadSeededRegistrationsAsync();

        registrations.Select(registration => registration.Id)
            .Should()
            .BeEquivalentTo([DevelopmentSeedTestData.FirstMachineId, DevelopmentSeedTestData.SecondMachineId]);
        registrations.Should().OnlyContain(registration => registration.CompanyId == DevelopmentSeedTestData.CompanyId);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithASeedMachineName_NamesTheRegistrationAndElseUsesTheMachineId()
    {
        await _fixture.RunDevelopmentSeedAsync();

        var registrations = await _fixture.LoadSeededRegistrationsAsync();

        registrations.Single(registration => registration.Id == DevelopmentSeedTestData.FirstMachineId).Name
            .Should().Be(DevelopmentSeedTestData.FirstMachineName);
        registrations.Single(registration => registration.Id == DevelopmentSeedTestData.SecondMachineId).Name
            .Should().Be(DevelopmentSeedTestData.SecondMachineId.ToString());
    }

    [Fact]
    public async Task SeedOptionalDataAsync_RunTwice_AddsNothing()
    {
        await _fixture.RunDevelopmentSeedAsync();
        var firstRun = await _fixture.LoadSeededRegistrationsAsync();

        await _fixture.RunDevelopmentSeedAsync();

        var secondRun = await _fixture.LoadSeededRegistrationsAsync();

        secondRun.Should().BeEquivalentTo(firstRun);
    }
}
