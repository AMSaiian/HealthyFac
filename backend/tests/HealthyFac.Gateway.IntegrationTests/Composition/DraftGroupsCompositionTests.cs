using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayProxyCollection.Name)]
public sealed class DraftGroupsCompositionTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string CoreGroupsJson = """
                                          {"groups":[
                                            {"machineId":"m1","machineName":"Lathe","machineDraft":{"id":"d1"}},
                                            {"machineId":null,"machineName":null,"machineDraft":{"id":"d2"}}]}
                                          """;

    private const string TelemetryJson = """
                                         {"metrics":[
                                           {"id":"t1","targetMachineId":"m1",
                                            "targetMachineDraftId":null,"proposalEntryId":"e1"},
                                           {"id":"t3","targetMachineId":"m9",
                                            "targetMachineDraftId":null,"proposalEntryId":"e3"}],
                                          "statuses":[
                                           {"id":"t2","targetMachineId":null,
                                            "targetMachineDraftId":"d2","proposalEntryId":"e2"},
                                           {"id":"t4","targetMachineId":null,
                                            "targetMachineDraftId":null,"proposalEntryId":"e5"}],
                                          "phases":[
                                           {"id":"p1","targetMachineId":"m1",
                                            "targetMachineDraftId":null,"proposalEntryId":"e1"}]}
                                         """;

    private const string NamesJson = """{"machines":[{"id":"m9","name":"Mill"}],"machineDrafts":[]}""";

    private readonly GatewayEnvironment _environment = environment;
    private readonly CompositionStubs _stubs = new();

    public async ValueTask InitializeAsync() => await _stubs.StartAsync();

    public async ValueTask DisposeAsync() => await _stubs.DisposeAsync();

    [Fact]
    public async Task GetAttemptDrafts_WithDraftsOfEveryKind_GroupsThemByMachineCreateDraftAndEntry()
    {
        _stubs.Core.Respond(HttpMethod.Get, CompositionStubs.CoreAttemptDrafts, status: 200, CoreGroupsJson);
        _stubs.Telemetry.Respond(HttpMethod.Get, CompositionStubs.TelemetryAttemptDrafts, status: 200, TelemetryJson);
        _stubs.Core.Respond(HttpMethod.Post, DraftSearchCompositionTestData.NamesPath, status: 200, NamesJson);
        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var response = await client.GetAsync(
            CompositionStubs.PublicAttemptDrafts,
            TestContext.Current.CancellationToken);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
        var groups = body[propertyName: "groups"]!.AsArray();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        groups.Should().HaveCount(expected: 4);
        Ids(groups[index: 0], list: "metrics").Should().Equal("t1");
        Ids(groups[index: 0], list: "phases").Should().Equal("p1");
        Ids(groups[index: 1], list: "statuses").Should().Equal("t2");
        groups[index: 2]![propertyName: "machineName"]!.GetValue<string>().Should().Be(expected: "Mill");
        Ids(groups[index: 2], list: "metrics").Should().Equal("t3");
        groups[index: 3]![propertyName: "machineName"].Should().BeNull();
        Ids(groups[index: 3], list: "statuses").Should().Equal("t4");
    }

    private static string[] Ids(JsonNode? group, string list) =>
        [.. group![list]!.AsArray().Select(item => item![propertyName: "id"]!.GetValue<string>())];
}
