using System.Text.Json;
using System.Text.Json.Nodes;
using HealthyFac.Telemetry.Infrastructure.MetricSchemas;
using HealthyFac.Telemetry.UnitTests.MetricSchemas.Data;
using Json.Schema;

namespace HealthyFac.Telemetry.UnitTests.MetricSchemas;

public sealed class MetricSchemaGeneratorTests
{
    [Fact]
    public void Evaluate_WithEveryKeyAndCorrectTypes_IsValid()
    {
        var schema = CreateSchema();

        var isValid = Evaluate(schema, MetricSchemaTestData.CreateMessage());

        isValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(
        nameof(MetricSchemaTestData.ValuesOutsideRangeAndStates),
        MemberType = typeof(MetricSchemaTestData)
    )]
    public void Evaluate_WithValueOutsideRangeOrStates_IsStillValid(double temperature, string mode)
    {
        var schema = CreateSchema();

        var isValid = Evaluate(schema, MetricSchemaTestData.CreateMessage(temperature, mode: mode));

        isValid.Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(MetricSchemaTestData.BrokenMessages), MemberType = typeof(MetricSchemaTestData))]
    public void Evaluate_WithBrokenMessage_IsInvalid(string json)
    {
        var schema = CreateSchema();

        var isValid = Evaluate(schema, JsonNode.Parse(json));

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithoutMetricsAndStatuses_IsValidForRevisionWithoutDefinitions()
    {
        var schema = new MetricSchemaGenerator().Generate(MetricSchemaTestData.GatewayId, revision: 0, []);

        var isValid = Evaluate(schema, MetricSchemaTestData.CreateEmptyMessage());

        isValid.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithMetricsOnMachineWithoutDefinitions_IsInvalid()
    {
        var schema = new MetricSchemaGenerator().Generate(MetricSchemaTestData.GatewayId, revision: 0, []);

        var isValid = Evaluate(schema, MetricSchemaTestData.CreateMessage());

        isValid.Should().BeFalse();
    }

    [Fact]
    public void Generate_Always_UsesDraft2020AndIdOfGatewayAndRevision()
    {
        var schema = JsonNode.Parse(CreateSchema())!;

        schema[propertyName: "$schema"]!.GetValue<string>().Should().Be(MetaSchemas.Draft202012Id.ToString());
        schema[propertyName: "$id"]!.GetValue<string>()
            .Should()
            .Be($"urn:healthyfac:gateways:{MetricSchemaTestData.GatewayId}:metrics:{MetricSchemaTestData.Revision}");
    }

    [Fact]
    public void Generate_Always_CarriesRangeStatesUnitAndTitleAsAnnotations()
    {
        var properties =
            JsonNode.Parse(CreateSchema())![propertyName: "properties"]![propertyName: "metrics"]![
                propertyName: "properties"]!;

        var temperature = properties[MetricSchemaTestData.TemperatureKey]!;
        var mode = properties[MetricSchemaTestData.ModeKey]!;

        temperature[propertyName: "title"]!.GetValue<string>().Should().Be(expected: "Spindle temperature");
        temperature[propertyName: "x-healthyfac-range"]![propertyName: "min"]!.GetValue<double>()
            .Should()
            .Be(expected: -20);
        temperature[propertyName: "x-healthyfac-range"]![propertyName: "max"]!.GetValue<double>()
            .Should()
            .Be(expected: 200);
        temperature[propertyName: "x-healthyfac-unit"]![propertyName: "quantity"]!.GetValue<string>()
            .Should()
            .Be(expected: "Temperature");
        temperature[propertyName: "x-healthyfac-unit"]![propertyName: "name"]!.GetValue<string>()
            .Should()
            .Be(expected: "DegreeCelsius");
        mode[propertyName: "x-healthyfac-states"]!.AsArray()
            .Select(state => state!.GetValue<string>())
            .Should()
            .Equal("auto", "manual", "off");
    }

    [Fact]
    public void Generate_ForMetricWithoutRange_WritesNoRangeAnnotation()
    {
        var properties =
            JsonNode.Parse(CreateSchema())![propertyName: "properties"]![propertyName: "metrics"]![
                propertyName: "properties"]!;

        var door = properties[MetricSchemaTestData.DoorKey]!.AsObject();

        door.ContainsKey(propertyName: "x-healthyfac-range").Should().BeFalse();
        door.ContainsKey(propertyName: "x-healthyfac-unit").Should().BeFalse();
    }

    private static string CreateSchema() =>
        new MetricSchemaGenerator().Generate(
            MetricSchemaTestData.GatewayId,
            MetricSchemaTestData.Revision,
            MetricSchemaTestData.Metrics);

    private static bool Evaluate(string schemaJson, JsonNode? message)
    {
        var registry = new SchemaRegistry();
        var schema = JsonSchema.FromText(schemaJson, new BuildOptions { SchemaRegistry = registry });
        var options = new EvaluationOptions { RequireFormatValidation = true };

        return schema.Evaluate(JsonSerializer.SerializeToElement(message), options).IsValid;
    }
}
