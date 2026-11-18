using System.Net;
using System.Text.Json;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;

namespace HealthyFac.Core.ApiTests.Drafts;

[Collection(CoreApiCollection.Name)]
public sealed class ReadDraftsApiTests(CoreApiFixture fixture)
{
    private const string SearchBody = "{\"limit\":25,\"filter\":{}}";

    private const string NewestFirstBody =
        "{\"limit\":25,\"sort\":{\"column\":\"proposedAt\",\"direction\":\"desc\"},\"filter\":{}}";

    private const string DismissedOnlyBody =
        "{\"limit\":25,\"filter\":{\"states\":{\"values\":[\"dismissed\"]}}}";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithSortOnProposedAt_ReturnsTheNewestProposalFirst()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-ORDER"));
        var older = await _fixture.ArrangeMachineDraftAsync(machineId);
        await Task.Delay(millisecondsDelay: 20, TestContext.Current.CancellationToken);
        var newer = await _fixture.ArrangeMachineDraftAsync(machineId);

        var items = await SearchAsync(machineId, NewestFirstBody);

        items.Select(item => item.GetProperty(propertyName: "id").GetString())
            .Should()
            .Equal(newer.EncryptedId, older.EncryptedId);
    }

    [Fact]
    public async Task Search_Default_ReturnsAllStatesWithPendingFirst()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-STATE"));
        var dismissed = await _fixture.ArrangeMachineDraftAsync(machineId);
        var pending = await _fixture.ArrangeMachineDraftAsync(machineId);
        await SendWriteAsync(DraftApiTestData.DismissPath(dismissed.EncryptedId));

        var items = await SearchAsync(machineId, SearchBody);

        items.Select(item => item.GetProperty(propertyName: "id").GetString())
            .Should()
            .Equal(pending.EncryptedId, dismissed.EncryptedId);
        items.Select(item => item.GetProperty(propertyName: "state").GetString())
            .Should()
            .Equal("pending", "dismissed");
    }

    [Fact]
    public async Task Search_WithLimitOne_PagesThroughEveryDraftOnceWithTheNextPosition()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-PAGES"));
        var first = await _fixture.ArrangeMachineDraftAsync(machineId);
        var second = await _fixture.ArrangeMachineDraftAsync(machineId);
        var third = await _fixture.ArrangeMachineDraftAsync(machineId);
        var seenIds = new List<string?>();
        var body = LimitOneBody(after: null);

        for (var page = 0; page < 3; page++)
        {
            var response = await SearchResponseAsync(machineId, body);
            using var document = JsonDocument.Parse(response);
            var items = document.RootElement.GetProperty(propertyName: "items").EnumerateArray().ToList();
            var next = document.RootElement.GetProperty(propertyName: "nextPosition");
            seenIds.Add(items.Single().GetProperty(propertyName: "id").GetString());
            body = LimitOneBody(next.ValueKind == JsonValueKind.Null ? null : next.GetRawText());
        }

        seenIds.Should().BeEquivalentTo([first.EncryptedId, second.EncryptedId, third.EncryptedId]);
        body.Should().Be(LimitOneBody(after: null));
    }

    [Fact]
    public async Task Search_WithAPositionThatDoesNotFitTheSortColumn_Returns400()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-BADPOS"));
        await _fixture.ArrangeMachineDraftAsync(machineId);
        var position = $"{{\"sortValue\":\"abc\",\"id\":\"{_fixture.IdEncryptor.Encrypt(Guid.CreateVersion7())}\"}}";

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            SearchPath(_fixture.IdEncryptor.Encrypt(machineId)),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission),
            LimitOneBody(position));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Search_WithStateFilter_ReturnsOnlyThoseStates()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "SEARCH-FILTER"));
        var dismissed = await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await SendWriteAsync(DraftApiTestData.DismissPath(dismissed.EncryptedId));

        var items = await SearchAsync(machineId, DismissedOnlyBody);

        items.Select(item => item.GetProperty(propertyName: "id").GetString()).Should().Equal(dismissed.EncryptedId);
    }

    [Fact]
    public async Task SearchForAttempt_WithDraftsOfSeveralMachines_ReturnsThemWithTheMachineOfEachRow()
    {
        var attemptId = Guid.CreateVersion7();
        var firstId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "ATTEMPT-1"));
        var secondId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "ATTEMPT-2"));
        var first = await _fixture.ArrangeMachineDraftAsync(firstId, attemptId: attemptId);
        var second = await _fixture.ArrangeMachineDraftAsync(secondId, attemptId: attemptId);

        var items = await SearchAttemptAsync(attemptId, SearchBody);

        var byId = items.ToDictionary(item => item.GetProperty(propertyName: "id").GetString()!);

        byId.Keys.Should().BeEquivalentTo(first.EncryptedId, second.EncryptedId);
        byId[first.EncryptedId]
            .GetProperty(propertyName: "machineName")
            .GetString()
            .Should()
            .Be(MachineApiTestData.MachineName);
        byId[second.EncryptedId]
            .GetProperty(propertyName: "machineId")
            .GetString()
            .Should()
            .Be(_fixture.IdEncryptor.Encrypt(secondId));
    }

    [Fact]
    public async Task SearchForAttempt_WithDraftsOfAnotherAttemptAndAnotherCompany_ListsOnlyTheAttemptOfTheCompany()
    {
        var attemptId = Guid.CreateVersion7();
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "ATTEMPT-SCOPE"));
        var inScope = await _fixture.ArrangeMachineDraftAsync(machineId, attemptId: attemptId);
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMachineDraftAsync(machineId, Guid.CreateVersion7(), attemptId);

        var items = await SearchAttemptAsync(attemptId, SearchBody);

        items.Select(item => item.GetProperty(propertyName: "id").GetString()).Should().Equal(inScope.EncryptedId);
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
    public async Task GetAttemptDrafts_GroupsTheUpdateDraftUnderItsMachineAndTheCreateDraftAlone()
    {
        var machineId =
            _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(serialNumber: "ATTEMPT-GROUPS"));
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMachineDraftAsync();

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Get,
            $"v1/ingest-attempts/{_fixture.IdEncryptor.Encrypt(DraftApiTestData.AttemptId)}/drafts",
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission));
        using var document = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        var groups = document.RootElement.GetProperty(propertyName: "groups").EnumerateArray().ToList();
        var existing = groups.Single(group =>
            group.GetProperty(propertyName: "machineId").ValueKind == JsonValueKind.String);
        var proposed = groups.Single(group =>
            group.GetProperty(propertyName: "machineId").ValueKind == JsonValueKind.Null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        groups.Should().HaveCount(expected: 2);
        existing.GetProperty(propertyName: "machineName").GetString().Should().Be(MachineApiTestData.MachineName);
        existing.GetProperty(propertyName: "machineDraft").ValueKind.Should().Be(JsonValueKind.Object);
        proposed.GetProperty(propertyName: "machineDraft").ValueKind.Should().Be(JsonValueKind.Object);
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

    private static string LimitOneBody(string? after) =>
        after is null
            ? "{\"limit\":1,\"filter\":{}}"
            : "{\"limit\":1,\"filter\":{},\"after\":" + after + "}";

    private static string SearchPath(string encryptedMachineId) =>
        $"v1/machines/{encryptedMachineId}/drafts/search";

    private async Task<string> SearchResponseAsync(Guid machineId, string body)
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            SearchPath(_fixture.IdEncryptor.Encrypt(machineId)),
            _fixture.CreateMachineClaims(DraftApiTestData.DraftReadPermission),
            body);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return content;
    }

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

    private async Task<List<JsonElement>> SearchAttemptAsync(Guid attemptId, string body)
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            $"v1/ingest-attempts/{_fixture.IdEncryptor.Encrypt(attemptId)}/drafts/search",
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
