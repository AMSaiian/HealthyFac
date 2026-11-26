using System.Net;
using System.Text.Json;
using HealthyFac.Core.ApiTests.Drafts.Data;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.ApiTests.Machines;

[Collection(CoreApiCollection.Name)]
[Feature(TestFeatures.Machines)]
public sealed class SearchMachinesApiTests(CoreApiFixture fixture)
{
    private const string WithoutFilterBody = "{\"offset\":0,\"limit\":25}";
    private const string AllBody = "{\"offset\":0,\"limit\":25,\"filter\":{}}";
    private const string WithDraftsBody = "{\"offset\":0,\"limit\":25,\"filter\":{\"hasPendingDrafts\":true}}";
    private const string WithoutDraftsBody = "{\"offset\":0,\"limit\":25,\"filter\":{\"hasPendingDrafts\":false}}";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithTwoPendingUpdateDrafts_CountsTwoPendingDrafts()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(serialNumber: "COUNT-TWO");
        var machineId = _fixture.IdEncryptor.Decrypt(encrypted);
        await _fixture.ArrangeMachineDraftAsync(machineId);
        await _fixture.ArrangeMachineDraftAsync(machineId);

        var counts = await ReadCountsAsync(AllBody);

        counts[encrypted].Should().Be(expected: 2);
    }

    [Fact]
    public async Task Search_Always_LeavesDismissedDraftsOutOfTheCount()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(serialNumber: "COUNT-STATES");
        var machineId = _fixture.IdEncryptor.Decrypt(encrypted);
        await _fixture.ArrangeMachineDraftAsync(machineId);
        var dismissed = await _fixture.ArrangeMachineDraftAsync(machineId);
        await SendWriteAsync(DraftApiTestData.DismissPath(dismissed.EncryptedId));

        var counts = await ReadCountsAsync(AllBody);

        counts[encrypted].Should().Be(expected: 1);
    }

    [Fact]
    public async Task Search_WithAPendingCreateDraft_DoesNotCountItOnAnyMachine()
    {
        var encrypted = await _fixture.ArrangeMachineAsync(serialNumber: "COUNT-CREATE");
        await _fixture.ArrangeMachineDraftAsync();

        var counts = await ReadCountsAsync(AllBody);

        counts[encrypted].Should().Be(expected: 0);
    }

    [Fact]
    public async Task Search_WithHasPendingDraftsFilter_SplitsTheMachines()
    {
        var withDrafts = await _fixture.ArrangeMachineAsync(serialNumber: "FILTER-WITH");
        var withoutDrafts = await _fixture.ArrangeMachineAsync(serialNumber: "FILTER-WITHOUT");
        await _fixture.ArrangeMachineDraftAsync(_fixture.IdEncryptor.Decrypt(withDrafts));

        var withCounts = await ReadCountsAsync(WithDraftsBody);
        var withoutCounts = await ReadCountsAsync(WithoutDraftsBody);

        withCounts.Keys.Should().Equal(withDrafts);
        withoutCounts.Keys.Should().Equal(withoutDrafts);
    }

    [Fact]
    public async Task Search_WithoutFilter_ReturnsFirstPageOfAllRows()
    {
        var first = await _fixture.ArrangeMachineAsync(serialNumber: "NOFILTER-A");
        var second = await _fixture.ArrangeMachineAsync(serialNumber: "NOFILTER-B");

        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission),
            WithoutFilterBody);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        using var document = JsonDocument.Parse(content);

        var ids = document.RootElement.GetProperty(propertyName: "items")
            .EnumerateArray()
            .Select(item => _fixture.IdEncryptor.Decrypt(item.GetProperty(propertyName: "id").GetString()!))
            .ToList();

        var totalCount = document.RootElement.GetProperty(propertyName: "totalCount").GetInt32();

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        ids.Should()
            .BeEquivalentTo([_fixture.IdEncryptor.Decrypt(first), _fixture.IdEncryptor.Decrypt(second)]);

        ids.Select(id => id.ToString()).Should().BeInAscendingOrder(StringComparer.Ordinal);
        totalCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task Search_WithInvalidFilterValue_Returns400WithRuleMessage()
    {
        var response = await _fixture.SendMachineRequestAsync(
            HttpMethod.Post,
            MachineApiTestData.SearchPath,
            _fixture.CreateMachineClaims(MachineApiTestData.ReadPermission),
            MachineApiTestData.SearchWithBackwardsGoLiveRangeBody);

        var content = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        content.Should().Contain(GridErrors.Validation.DateRangeFromNotBeforeTo);
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
