using HealthyFac.Seeder.Relay;
using HealthyFac.Seeder.UnitTests.Relay.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Seeder.UnitTests.Relay;

[Feature(TestFeatures.Seeding)]
[Feature(TestFeatures.Messaging)]
public sealed class MessageRelayTests
{
    [Fact(Explicit = true)]
    public async Task RunAsync_WithRowsOfOneSender_DeliversThemInTheirOrder()
    {
        var log = new List<string>();
        var sender = new FakeRelayEndpoint(service: "core", [], log);
        var receiver = new FakeRelayEndpoint(service: "telemetry", ["m1", "m2", "m3"], log);
        sender.AddPending(RelayTestData.Row(type: "m3", minute: 3));
        sender.AddPending(RelayTestData.Row(type: "m1", minute: 1));
        sender.AddPending(RelayTestData.Row(type: "m2", minute: 2));
        var relay = new MessageRelay([sender, receiver]);

        await relay.RunAsync(RelayTestData.Event, TestContext.Current.CancellationToken);

        log.Where(entry => entry.StartsWith(value: "telemetry received", StringComparison.Ordinal))
            .Should()
            .Equal("telemetry received m1", "telemetry received m2", "telemetry received m3");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithARowQueuedWhileDraining_DeliversItAfterEveryEarlierRow()
    {
        var log = new List<string>();
        var core = new FakeRelayEndpoint(service: "core", ["m3"], log);
        var telemetry = new FakeRelayEndpoint(service: "telemetry", ["m1", "m2"], log);
        core.AddPending(RelayTestData.Row(type: "m1", minute: 1));
        core.AddPending(RelayTestData.Row(type: "m2", minute: 2));

        telemetry.OnHandle = (row, endpoint) =>
        {
            if (row.Type == "m1")
            {
                endpoint.AddPending(RelayTestData.Row(type: "m3", minute: 3));
            }
        };

        var relay = new MessageRelay([telemetry, core]);

        await relay.RunAsync(RelayTestData.Event, TestContext.Current.CancellationToken);

        log.Where(entry => entry.Contains(value: "received", StringComparison.Ordinal))
            .Should()
            .Equal(
                "telemetry received m1",
                "telemetry received m2",
                "core received m3");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithATypeThatTwoEndpointsHandle_DeliversItToBoth()
    {
        var log = new List<string>();
        var sender = new FakeRelayEndpoint(service: "core", [], log);
        var first = new FakeRelayEndpoint(service: "telemetry", ["m1"], log);
        var second = new FakeRelayEndpoint(service: "insights", ["m1"], log);
        sender.AddPending(RelayTestData.Row(type: "m1", minute: 1));
        var relay = new MessageRelay([sender, first, second]);

        await relay.RunAsync(RelayTestData.Event, TestContext.Current.CancellationToken);

        log.Should().Contain(["telemetry received m1", "insights received m1", "core published m1"]);
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithARowWithoutReceiver_MarksItPublishedAndDeliversItToNobody()
    {
        var log = new List<string>();
        var sender = new FakeRelayEndpoint(service: "core", [], log);
        var other = new FakeRelayEndpoint(service: "telemetry", ["m2"], log);
        sender.AddPending(RelayTestData.Row(type: "m1", minute: 1));
        var relay = new MessageRelay([sender, other]);

        await relay.RunAsync(RelayTestData.Event, TestContext.Current.CancellationToken);

        log.Should().Equal("core published m1");
        sender.Pending.Should().BeEmpty();
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_WithAReceiverFailure_ThrowsAndMovesNoFurtherRow()
    {
        var log = new List<string>();
        var sender = new FakeRelayEndpoint(service: "core", [], log);
        var receiver = new FakeRelayEndpoint(service: "telemetry", ["m1", "m2"], log);
        receiver.FailOn(type: "m1", error: "The company is required.");
        sender.AddPending(RelayTestData.Row(type: "m1", minute: 1));
        sender.AddPending(RelayTestData.Row(type: "m2", minute: 2));
        var relay = new MessageRelay([sender, receiver]);

        var act = () => relay.RunAsync(RelayTestData.Event, TestContext.Current.CancellationToken);

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(
                expectedWildcardPattern: "*telemetry*m1*orders.json#order-1*The company is required.*");

        sender.Pending.Should().ContainSingle(row => row.Type == "m2");
    }

    [Fact(Explicit = true)]
    public async Task RunAsync_OnTwoIdenticalStates_DeliversTheSameSequence()
    {
        var firstLog = await RunScenarioAsync();

        var secondLog = await RunScenarioAsync();

        secondLog.Should().Equal(firstLog);
    }

    private static async Task<List<string>> RunScenarioAsync()
    {
        var log = new List<string>();
        var core = new FakeRelayEndpoint(service: "core", ["m3"], log);
        var telemetry = new FakeRelayEndpoint(service: "telemetry", ["m1", "m2"], log);
        core.AddPending(RelayTestData.Row(type: "m2", minute: 2));
        core.AddPending(RelayTestData.Row(type: "m1", minute: 1));

        telemetry.OnHandle = (row, endpoint) =>
        {
            if (row.Type == "m1")
            {
                endpoint.AddPending(RelayTestData.Row(type: "m3", minute: 3));
            }
        };

        await new MessageRelay([telemetry, core]).RunAsync(RelayTestData.Event, CancellationToken.None);

        return log;
    }
}
