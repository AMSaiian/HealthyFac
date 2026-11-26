using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;

namespace HealthyFac.Core.ApiTests.WorkOrders;

[Collection(CoreApiCollection.Name)]
public sealed class WorkOrderDraftApiTests(CoreApiFixture fixture)
{
    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Apply_WithBothPermissions_Returns200AndCreatesTheWorkOrder()
    {
        var draftId = await _fixture.ArrangeWorkOrderDraftAsync();

        var claims = _fixture.CreateDraftClaims(
            WorkOrderDraftApiTestData.DraftsWrite,
            WorkOrderDraftApiTestData.DraftsRead,
            WorkOrderApiTestData.WritePermission,
            WorkOrderApiTestData.ManagePermission,
            WorkOrderApiTestData.ReadPermission);

        var response = await _fixture.PostDraftAsync(claims, draftId, action: "apply", _fixture.CreateApplyBody());

        var body = (await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        var draft = await _fixture.ReadDraftAsync(claims, draftId);
        var order = await _fixture.ReadOrderAsync(claims, body["workOrderId"]!.GetValue<string>());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        draft["state"]!.GetValue<string>().Should().Be(expected: "applied");
        order["title"]!.GetValue<string>().Should().Be(WorkOrderApiTestData.Title);
    }

    [Theory]
    [MemberData(
        nameof(WorkOrderDraftApiTestData.ApplyWithMissingPermission),
        MemberType = typeof(WorkOrderDraftApiTestData))]
    public async Task Apply_WithOnlyOnePermission_Returns403(string caseName, string permission)
    {
        var draftId = await _fixture.ArrangeWorkOrderDraftAsync();
        var claims = _fixture.CreateDraftClaims(permission);

        var response = await _fixture.PostDraftAsync(claims, draftId, action: "apply", _fixture.CreateApplyBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Fact]
    public async Task Apply_WithAStaleRowVersion_Returns409()
    {
        var draftId = await _fixture.ArrangeWorkOrderDraftAsync();

        var claims = _fixture.CreateDraftClaims(
            WorkOrderDraftApiTestData.DraftsWrite,
            WorkOrderDraftApiTestData.WorkOrdersWrite,
            WorkOrderApiTestData.ManagePermission);

        var response = await _fixture.PostDraftAsync(
            claims,
            draftId,
            action: "apply",
            _fixture.CreateApplyBody(rowVersion: WorkOrderDraftApiTestData.StaleRowVersion));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Dismiss_Twice_Returns204ThenAnUnprocessableAnswer()
    {
        var draftId = await _fixture.ArrangeWorkOrderDraftAsync();
        var claims = _fixture.CreateDraftClaims(WorkOrderDraftApiTestData.DraftsWrite);

        var first = await _fixture.PostDraftAsync(
            claims, draftId, action: "dismiss", WorkOrderDraftApiTestData.EmptyObject);

        var second = await _fixture.PostDraftAsync(
            claims, draftId, action: "dismiss", WorkOrderDraftApiTestData.EmptyObject);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Dismiss_WithoutTheWritePermission_Returns403()
    {
        var draftId = await _fixture.ArrangeWorkOrderDraftAsync();
        var claims = _fixture.CreateDraftClaims(WorkOrderDraftApiTestData.DraftsRead);

        var response = await _fixture.PostDraftAsync(
            claims, draftId, action: "dismiss", WorkOrderDraftApiTestData.EmptyObject);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Search_WithTheReadPermission_ListsTheDraftAndWithoutItReturns403()
    {
        var draftId = await _fixture.ArrangeWorkOrderDraftAsync();
        var readClaims = _fixture.CreateDraftClaims(WorkOrderDraftApiTestData.DraftsRead);
        var otherClaims = _fixture.CreateDraftClaims(WorkOrderDraftApiTestData.DraftsWrite);
        const string body = """{"offset":0,"limit":50,"filter":{}}""";

        var allowed = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderDraftApiTestData.SearchPath, readClaims, body);

        var forbidden = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderDraftApiTestData.SearchPath, otherClaims, body);

        var page = (await allowed.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken))!;
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);

        page["items"]!.AsArray()
            .Select(item => item!["id"]!.GetValue<string>())
            .Should()
            .Contain(draftId);

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
