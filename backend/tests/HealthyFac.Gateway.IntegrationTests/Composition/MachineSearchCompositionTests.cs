using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayProxyCollection.Name)]
public sealed class MachineSearchCompositionTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private const string PublicPath = "/api/core/v1/machines/search";

    private const string CorePageJson = """
                                        {"items":[{"id":"m1","pendingDraftCount":1},
                                         {"id":"m2","pendingDraftCount":0}],"totalCount":2}
                                        """;

    private const string CountsJson = """{"counts":[{"machineId":"m1","count":2}]}""";

    private readonly GatewayEnvironment _environment = environment;
    private readonly CompositionStubs _stubs = new();

    public async ValueTask InitializeAsync()
    {
        await _stubs.StartAsync();
        _stubs.Core.Respond(HttpMethod.Post, CompositionStubs.CoreMachineSearch, status: 200, CorePageJson);
        _stubs.Telemetry.Respond(HttpMethod.Get, CompositionStubs.TelemetryPendingCounts, status: 200, CountsJson);
    }

    public async ValueTask DisposeAsync() => await _stubs.DisposeAsync();

    [Fact]
    public async Task Search_WithoutTheDraftFilter_AddsTheTelemetryCountToTheRowsAndCallsBoth()
    {
        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        var page = await PostAsync(client, json: """{"offset":0,"limit":25,"filter":{}}""");

        var counts = page[propertyName: "items"]!.AsArray()
            .ToDictionary(
                item => item![propertyName: "id"]!.GetValue<string>(),
                item => item![propertyName: "pendingDraftCount"]!.GetValue<int>());

        counts[key: "m1"].Should().Be(expected: 3);
        counts[key: "m2"].Should().Be(expected: 0);
        _stubs.Telemetry.Requests.Should().ContainSingle();
    }

    [Theory]
    [MemberData(
        nameof(MachineSearchCompositionTestData.DraftFilterValues),
        MemberType = typeof(MachineSearchCompositionTestData))]
    public async Task Search_WithTheDraftFilter_SendsCoreTheTelemetryMachinesNotTheClientValue(bool hasPendingDrafts)
    {
        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();
        var body = new JsonObject
        {
            [propertyName: "offset"] = 0,
            [propertyName: "limit"] = 25,
            [propertyName: "filter"] = new JsonObject
            {
                [propertyName: "hasPendingDrafts"] = hasPendingDrafts,
                [propertyName: "pendingDefinitionDraftMachineIds"] = new JsonArray("evil"),
            },
        };

        await PostAsync(client, body.ToJsonString());

        var filter = CoreFilter();
        var ids = filter[propertyName: "pendingDefinitionDraftMachineIds"]!.AsArray();

        ids.Select(id => id!.GetValue<string>()).Should().Equal("m1");
        filter[propertyName: "hasPendingDrafts"]!.GetValue<bool>().Should().Be(hasPendingDrafts);
    }

    [Fact]
    public async Task Search_WithoutTheDraftFilter_NeverSendsCoreTheClientMachineIds()
    {
        await using var gateway = _stubs.CreateGateway(_environment);
        using var client = gateway.CreateAuthenticatedClient();

        await PostAsync(
            client, json: """{"offset":0,"limit":25,"filter":{"pendingDefinitionDraftMachineIds":["evil"]}}""");

        CoreFilter().ContainsKey(propertyName: "pendingDefinitionDraftMachineIds").Should().BeFalse();
    }

    private JsonObject CoreFilter()
    {
        var request = _stubs.Core.Requests.Single(candidate => candidate.Path == CompositionStubs.CoreMachineSearch);

        return JsonNode.Parse(request.Body)![propertyName: "filter"]!.AsObject();
    }

    private static async Task<JsonNode> PostAsync(HttpClient client, string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, mediaType: "application/json");
        using var response = await client.PostAsync(PublicPath, content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }
}
