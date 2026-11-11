using System.Net;
using System.Text.Json;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;

namespace HealthyFac.Core.ApiTests.Drafts;

[Collection(CoreApiCollection.Name)]
public sealed class ReadDraftsApiTests(CoreApiFixture fixture)
{
    private const string SearchBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";

    private const string MetricOnlyBody =
        "{\"offset\":0,\"limit\":25,\"filter\":{\"kinds\":{\"values\":[\"metric\"]}}}";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithKindFilter_ReturnsOnlyThatKind()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-KIND"));
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);

        var items = await SearchAsync(machineId, MetricOnlyBody);

        items.Select(item => item.GetProperty(propertyName: "kind").GetString()).Should().Equal("metric");
    }

    [Fact]
    public async Task Search_Default_ReturnsTheNewestProposalFirst()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-ORDER"));
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await Task.Delay(millisecondsDelay: 20, TestContext.Current.CancellationToken);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);

        var items = await SearchAsync(machineId, SearchBody);

        items.Select(item => item.GetProperty(propertyName: "kind").GetString()).Should().Equal("metric", "machine");
    }

    [Fact]
    public async Task Search_Always_LeavesOutDismissedAndAppliedDrafts()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-STATE"));
        var pending = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        var dismissed = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        var applied = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        await SendWriteAsync(DraftApiTestData.DismissPath(dismissed.EncryptedId));
        await SendWriteAsync(DraftApiTestData.MarkAppliedPath, DraftApiTestData.MarkBody(applied.EncryptedId));

        var items = await SearchAsync(machineId, SearchBody);

        items.Select(item => item.GetProperty(propertyName: "id").GetString()).Should().Equal(pending.EncryptedId);
    }

    [Fact]
    public async Task Search_ForUnknownMachine_Returns404()
    {
        var unknown = _fixture.IdEncryptor.Encrypt(Guid.CreateVersion7());

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            SearchPath(unknown),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission),
            SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Search_WithoutReadPermission_Returns403()
    {
        var machineId = await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-DENIED");

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            SearchPath(machineId),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            SearchBody);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMachineDraft_ForUpdateDraft_ReturnsTheCurrentValuesAndRowVersion()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "GET-UPDATE"));
        var draft = await _fixture.ArrangeMachineDraftAsync(machineId);
        var rowVersion = await _fixture.LoadMachineRowVersionAsync(machineId);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{DraftApiTestData.MachineDraftsPath}/{draft.EncryptedId}",
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var current = document.RootElement.GetProperty(propertyName: "current");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.GetProperty(propertyName: "draft")
            .GetProperty(propertyName: "isCreate")
            .GetBoolean()
            .Should()
            .BeFalse();
        current.GetProperty(propertyName: "rowVersion").GetUInt32().Should().Be(rowVersion);
        current.GetProperty(propertyName: "serialNumber").GetString().Should().Be(expected: "GET-UPDATE");
    }

    [Fact]
    public async Task GetMachineDraft_OfAnotherCompany_Returns404()
    {
        var draft = await _fixture.ArrangeMachineDraftAsync(companyId: Guid.CreateVersion7());

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"{DraftApiTestData.MachineDraftsPath}/{draft.EncryptedId}",
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAttemptDrafts_GroupsChildrenWithoutAMachineDraftUnderTheirMachine()
    {
        var machineId =
            _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "ATTEMPT-GROUPS"));
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        await _fixture.ArrangeMachineDraftAsync(hasChildren: true);

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"v1/ingest-attempts/{_fixture.IdEncryptor.Encrypt(DraftApiTestData.AttemptId)}/drafts",
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var groups = document.RootElement.GetProperty(propertyName: "groups").EnumerateArray().ToList();
        var existing = groups.Single(group =>
            group.GetProperty(propertyName: "machineDraft").ValueKind == JsonValueKind.Null);
        var proposed = groups.Single(group =>
            group.GetProperty(propertyName: "machineDraft").ValueKind == JsonValueKind.Object);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        groups.Should().HaveCount(expected: 2);
        existing.GetProperty(propertyName: "machineName").GetString().Should().Be(MachineApiTestData.MachineName);
        existing.GetProperty(propertyName: "metrics").GetArrayLength().Should().Be(expected: 1);
        proposed.GetProperty(propertyName: "machineId").ValueKind.Should().Be(JsonValueKind.Null);
        proposed.GetProperty(propertyName: "metrics").GetArrayLength().Should().Be(expected: 1);
        proposed.GetProperty(propertyName: "statuses").GetArrayLength().Should().Be(expected: 1);
    }

    [Fact]
    public async Task GetAttemptDrafts_ForUnknownAttempt_ReturnsAnEmptyList()
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"v1/ingest-attempts/{_fixture.IdEncryptor.Encrypt(Guid.CreateVersion7())}/drafts",
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.GetProperty(propertyName: "groups").GetArrayLength().Should().Be(expected: 0);
    }

    private static string SearchPath(string encryptedMachineId) =>
        $"v1/machines/{encryptedMachineId}/drafts/search";

    private async Task<List<JsonElement>> SearchAsync(Guid machineId, string body)
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            SearchPath(_fixture.IdEncryptor.Encrypt(machineId)),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission),
            body);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(content);

        return
        [
            .. document.RootElement.GetProperty(propertyName: "items").EnumerateArray().Select(item => item.Clone()),
        ];
    }

    private Task<HttpResponseMessage> SendWriteAsync(string path, string? body = null) =>
        _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            path,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            body);
}
