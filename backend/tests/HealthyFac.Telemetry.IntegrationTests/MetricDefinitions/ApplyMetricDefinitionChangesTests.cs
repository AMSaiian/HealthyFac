using System.Text.Json.Nodes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Application.MachineRegistrations;
using HealthyFac.Telemetry.Application.MetricDefinitions;
using HealthyFac.Telemetry.Domain.MetricDefinitions;
using HealthyFac.Telemetry.IntegrationTests.Database.Data;
using HealthyFac.Telemetry.IntegrationTests.MachineRegistrations.Data;
using HealthyFac.Telemetry.IntegrationTests.MetricDefinitions.Data;
using HealthyFac.Telemetry.IntegrationTests.StatusDefinitions;

namespace HealthyFac.Telemetry.IntegrationTests.MetricDefinitions;

[Collection(TelemetryDefinitionsCollection.Name)]
public sealed class ApplyMetricDefinitionChangesTests(TelemetryDatabaseFixture fixture)
{
    private readonly TelemetryDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task Apply_WithCreatedUpdatedDeleted_StoresAllAndRaisesRevision()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewNumeric(), MetricDefinitionTestData.NewBoolean()]);
        var existing = await _fixture.LoadMetricRowsAsync(machineId);
        var temperature = existing.Single(metric => metric.Key == MetricDefinitionTestData.TemperatureKey);
        var door = existing.Single(metric => metric.Key == MetricDefinitionTestData.DoorKey);

        var response = await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 1,
            [MetricDefinitionTestData.NewStateSet()],
            [MetricDefinitionTestData.Update(temperature.Id, name: "Renamed", min: 0, max: 150)],
            [door.Id]);

        var rows = await _fixture.LoadMetricRowsAsync(machineId);
        var revisions = await _fixture.LoadSchemaRevisionsAsync(machineId);
        var registrationRevision = await _fixture.LoadMetricSchemaRevisionAsync(machineId);
        var schema = JsonNode.Parse(revisions.Last().Schema)!;
        var schemaKeys = schema[propertyName: "properties"]![propertyName: "metrics"]![propertyName: "properties"]!
            .AsObject()
            .Select(pair => pair.Key);

        response.Revision.Should().Be(expected: 2);
        registrationRevision.Should().Be(expected: 2);
        revisions.Select(revision => revision.Revision).Should().Equal(0, 1, 2);
        rows.Where(metric => metric.DeletedAt is null)
            .Select(metric => metric.Key)
            .Should()
            .Equal(MetricDefinitionTestData.ModeKey, MetricDefinitionTestData.TemperatureKey);
        rows.Single(metric => metric.Key == MetricDefinitionTestData.DoorKey).DeletedAt.Should().NotBeNull();
        rows.Single(metric => metric.Key == MetricDefinitionTestData.TemperatureKey)
            .Should()
            .BeEquivalentTo(new { Name = "Renamed", Min = 0.0, Max = 150.0 });
        schemaKeys.Should().Equal(MetricDefinitionTestData.ModeKey, MetricDefinitionTestData.TemperatureKey);
    }

    [Fact]
    public async Task Apply_WithBooleanAndStateSet_SetsDiscreteUnits()
    {
        var (tenant, machineId) = await ArrangeAsync();

        await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewBoolean(), MetricDefinitionTestData.NewStateSet()]);

        var units = await _fixture.LoadMetricUnitPairsAsync(machineId);
        var revisions = await _fixture.LoadSchemaRevisionsAsync(machineId);
        var schema = JsonNode.Parse(revisions.Last().Schema)!;
        var doorUnit = schema[propertyName: "properties"]![propertyName: "metrics"]![propertyName: "properties"]![
            MetricDefinitionTestData.DoorKey]![propertyName: "x-healthyfac-unit"]!;

        units.Should()
            .Equal(
                new KeyValuePair<string, string>(MetricDefinitionTestData.DoorKey, value: "Discrete.Boolean"),
                new KeyValuePair<string, string>(MetricDefinitionTestData.ModeKey, value: "Discrete.State"));
        doorUnit[propertyName: "quantity"]!.GetValue<string>().Should().Be(expected: "Discrete");
        doorUnit[propertyName: "name"]!.GetValue<string>().Should().Be(expected: "Boolean");
    }

    [Fact]
    public async Task Apply_WithStaleRevision_ReturnsConflictAndChangesNothing()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 5, [MetricDefinitionTestData.NewNumeric()]);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MetricDefinitionErrors.RevisionStale);
        var rows = await _fixture.LoadMetricRowsAsync(machineId);
        var revisions = await _fixture.LoadSchemaRevisionsAsync(machineId);

        rows.Should().BeEmpty();
        revisions.Should().ContainSingle();
    }

    [Fact]
    public async Task Apply_InParallelWithSameRevision_OneSucceedsOneConflicts()
    {
        var (tenant, machineId) = await ArrangeAsync();
        using var race = _fixture.SaveGate.StartRace(MetricDefinitionTestData.RaceParticipantCount);

        var failures = await Task.WhenAll(
            race.RunAsync(() => _fixture.ApplyMetricChangesAsync(
                tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()])),
            race.RunAsync(() => _fixture.ApplyMetricChangesAsync(
                tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewBoolean()])));

        var revision = await _fixture.LoadMetricSchemaRevisionAsync(machineId);
        var rows = await _fixture.LoadMetricRowsAsync(machineId);

        race.SaveCounts.Should().Equal(1, 1);
        failures.Count(failure => failure is null).Should().Be(expected: 1);
        failures.Single(failure => failure is not null).Should().BeOfType<ConflictException>();
        revision.Should().Be(expected: 1);
        rows.Should().ContainSingle();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.InvalidCreated), MemberType = typeof(MetricDefinitionTestData))]
    public async Task Apply_WithInvalidCreatedItem_ReturnsBadRequestWithItemPath(
        CreatedMetricDefinition created,
        string expectedPath
    )
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyMetricChangesAsync(tenant, machineId, expectedRevision: 0, [created]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        exception.Errors.Select(error => error.PropertyName).Should().Contain(expectedPath);
        (await _fixture.LoadMetricRowsAsync(machineId)).Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(MetricDefinitionTestData.ValidEdges), MemberType = typeof(MetricDefinitionTestData))]
    public async Task Apply_WithEdgeValues_Succeeds(CreatedMetricDefinition created)
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyMetricChangesAsync(tenant, machineId, expectedRevision: 0, [created]);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Apply_WithExistingActiveKey_ReturnsConflict()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 1, [MetricDefinitionTestData.NewNumeric()]);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(MetricDefinitionErrors.KeyTaken);
    }

    [Fact]
    public async Task Apply_WithKeyOfDeletedDefinition_Succeeds()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);
        var existing = (await _fixture.LoadMetricRowsAsync(machineId)).Single();
        await _fixture.ApplyMetricChangesAsync(tenant, machineId, expectedRevision: 1, deleted: [existing.Id]);

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 2, [MetricDefinitionTestData.NewNumeric()]);

        await act.Should().NotThrowAsync();
        var rows = await _fixture.LoadMetricRowsAsync(machineId);

        rows.Where(metric => metric.DeletedAt is null).Should().ContainSingle();
    }

    [Fact]
    public async Task Apply_WithDeleteAndCreateOfSameKeyInOneRequest_ReplacesTheDefinition()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);
        var existing = (await _fixture.LoadMetricRowsAsync(machineId)).Single();

        await _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 1,
            [MetricDefinitionTestData.NewBoolean(MetricDefinitionTestData.TemperatureKey)],
            deleted: [existing.Id]);

        var rows = await _fixture.LoadMetricRowsAsync(machineId);

        rows.Where(metric => metric.DeletedAt is null).Single().ValueKind.Should().Be(MetricValueKind.Boolean);
    }

    [Fact]
    public async Task Apply_WithDuplicateKeysInRequest_ReturnsBadRequest()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 0,
            [MetricDefinitionTestData.NewNumeric(), MetricDefinitionTestData.NewNumeric()]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        exception.Errors.Should().Contain(error => error.ErrorMessage == MetricDefinitionErrors.KeyDuplicatedInRequest);
    }

    [Fact]
    public async Task Apply_WithSameIdInUpdatedAndDeleted_ReturnsBadRequest()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);
        var existing = (await _fixture.LoadMetricRowsAsync(machineId)).Single();

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 1,
            updated: [MetricDefinitionTestData.Update(existing.Id, name: "Name")],
            deleted: [existing.Id]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        exception.Errors.Should().Contain(error => error.ErrorMessage == MetricDefinitionErrors.IdDuplicatedInRequest);
    }

    [Fact]
    public async Task Apply_WithUnknownUnitPair_ReturnsBadRequest()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var created = MetricDefinitionTestData.NewNumeric() with
        {
            Unit = new MetricUnitReference { QuantityKind = "Temperature", Name = "Parsec" },
        };

        var act = () => _fixture.ApplyMetricChangesAsync(tenant, machineId, expectedRevision: 0, [created]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        exception.Errors.Should().ContainSingle(error => error.PropertyName == "created[0].unit");
    }

    [Fact]
    public async Task Apply_WithUnitOfOtherQuantityKindWithSameName_UsesThePair()
    {
        var (tenant, machineId) = await ArrangeAsync();
        var created = MetricDefinitionTestData.NewNumeric() with
        {
            Unit = new MetricUnitReference { QuantityKind = "RelativeHumidity", Name = "Percent" },
            Min = 0,
            Max = 100,
        };

        await _fixture.ApplyMetricChangesAsync(tenant, machineId, expectedRevision: 0, [created]);

        var revisions = await _fixture.LoadSchemaRevisionsAsync(machineId);
        var schema = JsonNode.Parse(revisions.Last().Schema)!;
        var unit =
            schema[propertyName: "properties"]![propertyName: "metrics"]![propertyName: "properties"]![
                MetricDefinitionTestData.TemperatureKey]![
                propertyName: "x-healthyfac-unit"]!;

        unit[propertyName: "quantity"]!.GetValue<string>().Should().Be(expected: "RelativeHumidity");
        unit[propertyName: "name"]!.GetValue<string>().Should().Be(expected: "Percent");
    }

    [Fact]
    public async Task Apply_WithUnknownMetricId_ThrowsNotFoundAndChangesNothing()
    {
        var (tenant, machineId) = await ArrangeAsync();

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, deleted: [Guid.CreateVersion7()]);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MetricDefinitionErrors.MetricNotFound);
        (await _fixture.LoadMetricSchemaRevisionAsync(machineId)).Should().Be(expected: 0);
    }

    [Fact]
    public async Task Apply_WithRangeOnUpdateOfBoolean_ReturnsBadRequest()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewBoolean()]);
        var existing = (await _fixture.LoadMetricRowsAsync(machineId)).Single();

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant,
            machineId,
            expectedRevision: 1,
            updated: [MetricDefinitionTestData.Update(existing.Id, name: "Door", min: 1)]);

        var exception = (await act.Should().ThrowAsync<AppValidationException>()).Which;
        exception.Errors.Should().ContainSingle(error => error.PropertyName == "updated[0].min");
    }

    [Fact]
    public async Task Apply_ForMachineOfOtherCompany_ThrowsNotFound()
    {
        var (_, machineId) = await ArrangeAsync();
        var otherTenant = _fixture.ArrangeMachineTenant();

        var act = () => _fixture.ApplyMetricChangesAsync(
            otherTenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task Apply_ForDeletedMachine_ThrowsNotFound()
    {
        var (tenant, machineId) = await ArrangeAsync();
        await _fixture.DeleteMachineAsync(tenant.CompanyId, machineId);

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant, machineId, expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    [Fact]
    public async Task Apply_ForUnknownMachine_ThrowsNotFound()
    {
        var tenant = _fixture.ArrangeMachineTenant();

        var act = () => _fixture.ApplyMetricChangesAsync(
            tenant, Guid.CreateVersion7(), expectedRevision: 0, [MetricDefinitionTestData.NewNumeric()]);

        await act.Should().ThrowAsync<NotFoundException>().WithMessage(MachineRegistrationErrors.MachineNotFound);
    }

    private async Task<(MachineTenant Tenant, Guid MachineId)> ArrangeAsync()
    {
        var tenant = _fixture.ArrangeMachineTenant();
        var machineId = await _fixture.RegisterTenantMachineAsync(tenant.CompanyId);

        return (tenant, machineId);
    }
}
