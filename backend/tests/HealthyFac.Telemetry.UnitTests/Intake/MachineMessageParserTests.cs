using HealthyFac.Telemetry.Infrastructure.Intake;
using HealthyFac.Telemetry.Infrastructure.MetricSchemas;
using HealthyFac.Telemetry.UnitTests.Intake.Data;

namespace HealthyFac.Telemetry.UnitTests.Intake;

public sealed class MachineMessageParserTests
{
    private readonly MachineMessageParser _parser = new();

    [Theory]
    [MemberData(
        nameof(MachineMessageParserTestData.LenientSampledAtValues),
        MemberType = typeof(MachineMessageParserTestData))]
    public void Parse_WithSampledAtThatTheSchemaAccepts_StoresItAsUtc(string sampledAt)
    {
        var schemaJson = new MetricSchemaGenerator().Generate(MachineMessageParserTestData.GatewayId, revision: 1, []);
        var payload = string.Format(MachineMessageParserTestData.PayloadFormat, sampledAt);

        var result = _parser.Parse(schemaJson, payload, MachineMessageParserTestData.ReceivedAt);

        result.Errors.Should().BeEmpty();
        result.SampledAt.Should().Be(MachineMessageParserTestData.SampledAt);
    }
}
