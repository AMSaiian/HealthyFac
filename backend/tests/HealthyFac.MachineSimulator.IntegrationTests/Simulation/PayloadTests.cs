using System.Text.Json;
using HealthyFac.MachineSimulator.IntegrationTests.Simulation.Data;
using HealthyFac.Shared.Application.Observability;
using HealthyFac.Shared.Infrastructure.Messaging.Transport;
using HealthyFac.Testing.Features;
using HealthyFac.Testing.Tracing;

namespace HealthyFac.MachineSimulator.IntegrationTests.Simulation;

[Collection(SimulatorCollection.Name)]
[Feature(TestFeatures.Simulation)]
public sealed class PayloadTests(SimulatorFixture fixture)
{
    private readonly SimulatorFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(SimulatorTestData.AllSerialNumbers), MemberType = typeof(SimulatorTestData))]
    public async Task Tick_ForEachScenario_SendsAMessageThatTheTelemetryParserAccepts(string serialNumber)
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(serialNumber);
        var messages = await _fixture.TickAsync();

        var errors = await _fixture.ParseAsync(machine, messages.Single());

        errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Tick_WithRunningMachine_PublishesToTheTopicOfItsGateway()
    {
        var machine = await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);

        var messages = await _fixture.TickAsync();

        messages.Single().Topic.Should().Be($"healthyfac/gateways/{machine.GatewayId}/messages");
    }

    [Fact]
    public async Task Clock_AdvancedByOneSecondThreeTimes_PublishesOneMessageEachTime()
    {
        await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);
        _fixture.Subscriber.Clear();
        List<string> expectedTimes = [];

        for (var second = 1; second <= 3; second++)
        {
            _fixture.Clock.Advance(TimeSpan.FromSeconds(seconds: 1));
            expectedTimes.Add(_fixture.Clock.GetUtcNow().UtcDateTime.ToString(format: "O"));
            await _fixture.Subscriber.WaitForAsync(second, SimulatorTestData.WaitLimit);
        }

        var times = _fixture.Subscriber.Messages
            .Select(message => JsonDocument.Parse(message.Payload)
                .RootElement.GetProperty(propertyName: "sampledAt")
                .GetDateTime())
            .Select(time => time.ToUniversalTime().ToString(format: "O"))
            .ToList();

        times.Should().Equal(expectedTimes);
    }

    [Fact]
    public async Task Tick_WithRunningMachine_SendsTheTraceParentOfTheProducerSpan()
    {
        using var spans = new SpanCapture(ObservabilityConstants.Simulator.Source);
        await _fixture.ArrangeRunningMachineAsync(SimulatorTestData.GenericSerialNumber);

        var messages = await _fixture.TickAsync();

        var span = (await spans.WaitForSpansAsync(
            candidate => candidate.DisplayName == ObservabilityConstants.Simulator.PublishSpanName,
            expectedCount: 1)).Single();

        span.Kind.Should().Be(System.Diagnostics.ActivityKind.Producer);
        messages.Single().UserProperties[MessageHeaders.TraceParent].Should().Be(span.Id);
    }
}
