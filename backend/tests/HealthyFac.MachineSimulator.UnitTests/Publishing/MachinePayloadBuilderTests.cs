using System.Text.Json;
using HealthyFac.MachineSimulator.Publishing;
using HealthyFac.MachineSimulator.UnitTests.Publishing.Data;

namespace HealthyFac.MachineSimulator.UnitTests.Publishing;

public sealed class MachinePayloadBuilderTests
{
    [Fact]
    public void Build_WithDefinedKeysAndCodes_WritesTheEnvelopeWithUtcTime()
    {
        var sample = MachinePayloadBuilderTestData.Sample(
            new Dictionary<string, object> { [key: "speed"] = 12.5, [key: "running"] = true, [key: "mode"] = "auto" },
            "S002");

        var payload = MachinePayloadBuilder.Build(
            sample,
            MachinePayloadBuilderTestData.Definitions,
            MachinePayloadBuilderTestData.SampledAt);

        using var document = JsonDocument.Parse(payload.Json);
        var root = document.RootElement;
        root.GetProperty(propertyName: "sampledAt").GetString().Should().Be(expected: "2026-03-10T23:30:05.0000000Z");
        root.GetProperty(propertyName: "metrics")
            .GetProperty(propertyName: "speed")
            .GetDouble()
            .Should()
            .Be(expected: 12.5);
        root.GetProperty(propertyName: "metrics").GetProperty(propertyName: "running").GetBoolean().Should().BeTrue();
        root.GetProperty(propertyName: "metrics")
            .GetProperty(propertyName: "mode")
            .GetString()
            .Should()
            .Be(expected: "auto");
        root.GetProperty(propertyName: "statuses")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Should()
            .Equal("S002");
        payload.SkippedKeys.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithUndefinedKeyAndUndefinedCode_DropsBoth()
    {
        var sample = MachinePayloadBuilderTestData.Sample(
            new Dictionary<string, object> { [key: "speed"] = 1d, [key: "unknown"] = 2d },
            "S002",
            "X999");

        var payload = MachinePayloadBuilder.Build(
            sample,
            MachinePayloadBuilderTestData.Definitions,
            MachinePayloadBuilderTestData.SampledAt);

        using var document = JsonDocument.Parse(payload.Json);
        document.RootElement.GetProperty(propertyName: "metrics")
            .EnumerateObject()
            .Select(item => item.Name)
            .Should()
            .Equal("speed");
        document.RootElement.GetProperty(propertyName: "statuses")
            .EnumerateArray()
            .Select(item => item.GetString())
            .Should()
            .Equal("S002");
        payload.SkippedKeys.Should().BeEmpty();
    }

    [Fact]
    public void Build_WithValueOfAnotherKind_DropsTheKeyAndReportsIt()
    {
        var sample = MachinePayloadBuilderTestData.Sample(
            new Dictionary<string, object> { [key: "speed"] = "fast", [key: "running"] = true });

        var payload = MachinePayloadBuilder.Build(
            sample,
            MachinePayloadBuilderTestData.Definitions,
            MachinePayloadBuilderTestData.SampledAt);

        using var document = JsonDocument.Parse(payload.Json);
        document.RootElement.GetProperty(propertyName: "metrics")
            .EnumerateObject()
            .Select(item => item.Name)
            .Should()
            .Equal("running");
        payload.SkippedKeys.Should().Equal("speed");
    }

    [Fact]
    public void Build_WithNonUtcTime_WritesTheUtcInstant()
    {
        var local = new DateTime(year: 2026, month: 3, day: 11, hour: 1, minute: 30, second: 5, DateTimeKind.Local);

        var payload = MachinePayloadBuilder.Build(
            MachinePayloadBuilderTestData.Sample(new Dictionary<string, object>()),
            MachinePayloadBuilderTestData.Definitions,
            local);

        using var document = JsonDocument.Parse(payload.Json);
        document.RootElement.GetProperty(propertyName: "sampledAt").GetString().Should().EndWith(expected: "Z");
    }
}
