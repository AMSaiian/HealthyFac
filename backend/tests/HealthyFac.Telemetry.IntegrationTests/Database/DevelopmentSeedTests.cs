using System.Text.Json.Nodes;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;

namespace HealthyFac.Telemetry.IntegrationTests.Database;

public sealed class DevelopmentSeedTests(TelemetryDatabaseFixture fixture) : IClassFixture<TelemetryDatabaseFixture>
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_CreatesRegistrationsAndStatuses()
    {
        await _fixture.RunDevelopmentSeedAsync();

        var registrations = await _fixture.LoadSeededRegistrationsAsync();

        registrations.Select(registration => registration.Id)
            .Should()
            .BeEquivalentTo([DevelopmentSeedTestData.FirstMachineId, DevelopmentSeedTestData.SecondMachineId]);
        registrations.Single(registration => registration.Id == DevelopmentSeedTestData.FirstMachineId)
            .Should()
            .BeEquivalentTo(
                new
                {
                    GatewayId = DevelopmentSeedTestData.FirstGatewayId,
                    DepartmentId = DevelopmentSeedTestData.FirstDepartmentId,
                    StatusDefinitionCount = DevelopmentSeedTestData.StatusDefinitionsPerMachine,
                });
        registrations.Single(registration => registration.Id == DevelopmentSeedTestData.SecondMachineId)
            .DepartmentId.Should()
            .Be(DevelopmentSeedTestData.SecondDepartmentId);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_StoresRevision0Schema()
    {
        await _fixture.RunDevelopmentSeedAsync();

        var revisions = await _fixture.LoadSchemaRevisionsAsync(DevelopmentSeedTestData.FirstMachineId);

        revisions.Select(revision => revision.Revision).Should().Equal(0, 1);
    }

    [Fact]
    public async Task SeedOptionalDataAsync_WithSeedMachines_CreatesSampleMetricsAtRevision1()
    {
        await _fixture.RunDevelopmentSeedAsync();

        var metrics = await _fixture.LoadMetricRowsAsync(DevelopmentSeedTestData.FirstMachineId);
        var revision = await _fixture.LoadMetricSchemaRevisionAsync(DevelopmentSeedTestData.FirstMachineId);
        var revisions = await _fixture.LoadSchemaRevisionsAsync(DevelopmentSeedTestData.FirstMachineId);
        var schemaKeys =
            JsonNode.Parse(revisions.Last().Schema)![propertyName: "properties"]![propertyName: "metrics"]![
                    propertyName: "properties"]!
                .AsObject()
                .Select(pair => pair.Key);

        revision.Should().Be(expected: 1);
        metrics.Select(metric => metric.Key).Should().Equal("door.open", "spindle.mode", "spindle.temperature");
        metrics.Single(metric => metric.Key == "spindle.temperature")
            .Should()
            .BeEquivalentTo(new { Min = -20.0, Max = 200.0 });
        schemaKeys.Should().BeEquivalentTo("door.open", "spindle.mode", "spindle.temperature");
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
