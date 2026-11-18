using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Gateway.IntegrationTests.Composition.Data;
using HealthyFac.Gateway.IntegrationTests.Hosting;
using static HealthyFac.Gateway.IntegrationTests.Composition.Data.DraftSearchCompositionTestData;

namespace HealthyFac.Gateway.IntegrationTests.Composition;

[Collection(GatewayProxyCollection.Name)]
public sealed class DraftSearchCompositionTests(GatewayEnvironment environment) : IAsyncLifetime
{
    private readonly GatewayEnvironment _environment = environment;
    private readonly StubApiServer _core = new();
    private readonly StubApiServer _telemetry = new();

    public async ValueTask InitializeAsync()
    {
        await _core.StartAsync();
        await _telemetry.StartAsync();
        DraftSourceStub.Serve(_core, CoreSearchPath, CoreRows);
        DraftSourceStub.Serve(
            _telemetry,
            TelemetrySearchPath,
            TelemetryRows);
        _core.Respond(
            HttpMethod.Post,
            NamesPath,
            status: 200,
            NamesJson);
    }

    public async ValueTask DisposeAsync()
    {
        await _core.DisposeAsync();
        await _telemetry.DisposeAsync();
    }

    [Fact]
    public async Task Search_ReadingPageByPage_ReturnsEveryRowOnceAndCallsTheInternalPaths()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();
        var ids = new List<string>();
        JsonNode? after = null;

        do
        {
            var page = await PostAsync(
                client, new JsonObject
                {
                    [propertyName: "limit"] = PageLimit,
                    [propertyName: "after"] = after?.DeepClone(),
                });
            ids.AddRange(
                page[propertyName: "items"]!.AsArray().Select(item => item![propertyName: "id"]!.GetValue<string>()));
            after = page[propertyName: "nextPosition"];
        }
        while (after is not null && ids.Count <= MergedIds.Length);

        ids.Should().Equal(MergedIds);
        _core.Requests.Select(request => request.Path)
            .Should()
            .OnlyContain(path => path == CoreSearchPath || path == NamesPath);
        _telemetry.Requests.Select(request => request.Path)
            .Should()
            .OnlyContain(path => path == TelemetrySearchPath);
    }

    [Fact]
    public async Task Search_ReadingAllPagesOfALongList_KeepsTheTotalCountAndCallsEachSourceOncePerPage()
    {
        DraftSourceStub.Serve(_core, CoreSearchPath, LongListCoreRows);
        DraftSourceStub.Serve(_telemetry, TelemetrySearchPath, LongListTelemetryRows);
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();
        var totals = new List<int>();
        var ids = new List<string>();
        JsonNode? after = null;

        do
        {
            var page = await PostAsync(
                client, new JsonObject
                {
                    [propertyName: "limit"] = LongListLimit,
                    [propertyName: "after"] = after?.DeepClone(),
                });
            totals.Add(page[propertyName: "totalCount"]!.GetValue<int>());
            ids.AddRange(
                page[propertyName: "items"]!.AsArray().Select(item => item![propertyName: "id"]!.GetValue<string>()));
            after = page[propertyName: "nextPosition"];
        }
        while (after is not null);

        var coreLimits = _core.Requests
            .Where(request => request.Path == CoreSearchPath)
            .Select(request => JsonNode.Parse(request.Body)![propertyName: "limit"]!.GetValue<int>())
            .ToList();

        totals.Should().Equal(Enumerable.Repeat(LongListTotal, totals.Count));
        totals.Should().HaveCount(expected: 3);
        ids.Should().OnlyHaveUniqueItems().And.HaveCount(LongListTotal);
        coreLimits.Should().Equal(LongListLimit, 1, 1);
        _telemetry.Requests.Should().HaveCount(expected: 3);
    }

    [Fact]
    public async Task Search_WithTheMachineKindOnlyOnEveryPage_SendsNoTelemetryRequest()
    {
        DraftSourceStub.Serve(_core, CoreSearchPath, LongListCoreRows);
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();
        JsonNode? after = null;

        do
        {
            var page = await PostAsync(
                client, new JsonObject
                {
                    [propertyName: "limit"] = PageLimit,
                    [propertyName: "filter"] = JsonNode.Parse(json: """{"kinds":{"values":["machine"]}}"""),
                    [propertyName: "after"] = after?.DeepClone(),
                });
            after = page[propertyName: "nextPosition"];
        }
        while (after is not null);

        _telemetry.Requests.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(Limits), MemberType = typeof(DraftSearchCompositionTestData))]
    public async Task Search_OfTheFirstPage_CallsEachSourceOnceAndTheNamesAtMostOnce(int limit)
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        await PostAsync(client, new JsonObject { [propertyName: "limit"] = limit });

        _core.Requests.Count(request => request.Path == CoreSearchPath)
            .Should()
            .Be(expected: 1);
        _telemetry.Requests.Should().ContainSingle();
        _core.Requests.Count(request => request.Path == NamesPath)
            .Should()
            .BeLessThanOrEqualTo(expected: 1);
    }

    [Fact]
    public async Task Search_WithTelemetryRows_FillsTheMachineNamesFromTheMachineAndTheCreateDraft()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var page = await PostAsync(client, new JsonObject { [propertyName: "limit"] = 50 });

        var names = page[propertyName: "items"]!.AsArray()
            .ToDictionary(
                item => item![propertyName: "id"]!.GetValue<string>(),
                item => item![propertyName: "machineName"]?.GetValue<string>());

        names[key: "t1"].Should().Be(KnownMachineName);
        names[key: "t2"].Should().Be(CreateDraftName);
        names[key: "c1"].Should().Be(expected: "Core machine");
        page[propertyName: "totalCount"]!.GetValue<int>().Should().Be(expected: 7);
    }

    [Fact]
    public async Task Search_WithTheMachineKindOnly_CallsNoTelemetryAndOnlyCoreRows()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        var page = await PostAsync(
            client, new JsonObject
            {
                [propertyName: "limit"] = 50,
                [propertyName: "filter"] = JsonNode.Parse(json: """{"kinds":{"values":["machine"]}}"""),
            });

        _telemetry.Requests.Should().BeEmpty();
        _core.Requests.Count(request => request.Path == NamesPath).Should().Be(expected: 0);
        page[propertyName: "items"]!.AsArray().Should().HaveCount(expected: 3);
    }

    [Fact]
    public async Task Search_WithoutTheMachineKind_CallsNoCoreRows()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();

        await PostAsync(
            client, new JsonObject
            {
                [propertyName: "limit"] = 50,
                [propertyName: "filter"] = JsonNode.Parse(json: """{"kinds":{"values":["metric","status"]}}"""),
            });

        _core.Requests.Count(request => request.Path == CoreSearchPath)
            .Should()
            .Be(expected: 0);
        _telemetry.Requests.Should().ContainSingle();
    }

    [Fact]
    public async Task Search_WithTheAwaitingTargetState_SendsCoreNoAwaitingTargetAndReturns200()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();
        using var content = JsonContent.Create(
            new JsonObject
            {
                [propertyName: "limit"] = 50,
                [propertyName: "filter"] =
                    JsonNode.Parse(json: """{"states":{"values":["pending","awaitingTarget"]}}"""),
            });

        var response = await client.PostAsync(
            PublicSearchPath, content, TestContext.Current.CancellationToken);

        var coreBody = JsonNode.Parse(_core.Requests.First(request => request.Path == CoreSearchPath).Body)!;

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        coreBody[propertyName: "filter"]![propertyName: "states"]![propertyName: "values"]!.AsArray()
            .Select(value => value!.GetValue<string>())
            .Should()
            .Equal("pending");
    }

    [Fact]
    public async Task Search_WithoutAToken_Returns401()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateClient();
        using var content = JsonContent.Create(new JsonObject { [propertyName: "limit"] = 5 });

        var response = await client.PostAsync(
            PublicSearchPath, content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_WithAnInvalidLimit_Returns400WithoutCallingAService()
    {
        await using var gateway = CreateGateway();
        using var client = gateway.CreateAuthenticatedClient();
        using var content = JsonContent.Create(new JsonObject { [propertyName: "limit"] = 0 });

        var response = await client.PostAsync(
            PublicSearchPath, content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _core.Requests.Should().BeEmpty();
        _telemetry.Requests.Should().BeEmpty();
    }

    private GatewayFactory CreateGateway() =>
        _environment.CreateGateway(
            _core.Address,
            settings: new Dictionary<string, string>
            {
                [GatewaySettingKeys.TelemetryAddress] = _telemetry.Address,
                [GatewaySettingKeys.TelemetryHealthAddress] = _telemetry.Address,
            });

    private static async Task<JsonNode> PostAsync(HttpClient client, JsonObject body)
    {
        using var content = JsonContent.Create(body);
        using var response = await client.PostAsync(
            PublicSearchPath, content, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!;
    }
}
