using System.Net;
using System.Text.Json;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;

namespace HealthyFac.Core.ApiTests.Machines;

[Collection(CoreApiCollection.Name)]
public sealed class SearchMachinesApiTests(CoreApiFixture fixture)
{
    private const string AllBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
    private const string WithDraftsBody = "{\"offset\":0,\"limit\":25,\"filter\":{\"hasPendingDrafts\":true}}";
    private const string WithoutDraftsBody = "{\"offset\":0,\"limit\":25,\"filter\":{\"hasPendingDrafts\":false}}";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithUpdateDraftAndTwoChildren_CountsThreePendingDrafts()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(serialNumber: "COUNT-THREE");
        var machineId = _fixture.IdEncryptor.Decrypt(encrypted);
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);

        var counts = await ReadCountsAsync(AllBody);

        counts[encrypted].Should().Be(expected: 3);
    }

    [Fact]
    public async Task Search_Always_LeavesDismissedAndAppliedDraftsOutOfTheCount()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(serialNumber: "COUNT-STATES");
        var machineId = _fixture.IdEncryptor.Decrypt(encrypted);
        await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        var dismissed = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        var applied = await _fixture.ArrangeMetricDraftAsync(machineId, targetMachineDraftId: null);
        await SendWriteAsync(DraftApiTestData.DismissPath(dismissed.EncryptedId));
        await SendWriteAsync(DraftApiTestData.MarkAppliedPath, DraftApiTestData.MarkBody(applied.EncryptedId));

        var counts = await ReadCountsAsync(AllBody);

        counts[encrypted].Should().Be(expected: 1);
    }

    [Fact]
    public async Task Search_WithChildrenOfAPendingCreateDraft_DoesNotCountThem()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(serialNumber: "COUNT-CREATE");
        await _fixture.ArrangeMachineDraftAsync(hasChildren: true);

        var counts = await ReadCountsAsync(AllBody);

        counts[encrypted].Should().Be(expected: 0);
    }

    [Fact]
    public async Task Search_WithHasPendingDraftsFilter_SplitsTheMachines()
    {
        var withDrafts = await _fixture.ArrangeMachineAsync(serialNumber: "FILTER-WITH");
        var withoutDrafts = await _fixture.ArrangeMachineAsync(serialNumber: "FILTER-WITHOUT");
        await _fixture.ArrangeMetricDraftAsync(
            _fixture.IdEncryptor.Decrypt(withDrafts), targetMachineDraftId: null);

        var withCounts = await ReadCountsAsync(WithDraftsBody);
        var withoutCounts = await ReadCountsAsync(WithoutDraftsBody);

        withCounts.Keys.Should().Equal(withDrafts);
        withoutCounts.Keys.Should().Equal(withoutDrafts);
    }

    private async Task<Dictionary<string, int>> ReadCountsAsync(string body)
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission),
            body);
        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(content);

        return document.RootElement.GetProperty(propertyName: "items")
            .EnumerateArray()
            .ToDictionary(
                item => item.GetProperty(propertyName: "id").GetString()!,
                item => item.GetProperty(propertyName: "pendingDraftCount").GetInt32());
    }

    private Task<HttpResponseMessage> SendWriteAsync(string path, string? body = null) =>
        _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            path,
            _fixture.CreateMachineClaims(DraftApiTestData.DraftWritePermission),
            body);
}
