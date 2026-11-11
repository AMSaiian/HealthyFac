using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;

namespace HealthyFac.Core.ApiTests.WorkOrders;

[Collection(CoreApiCollection.Name)]
public sealed class WorkOrderInputTests(CoreApiFixture fixture)
{
    private const string StartTime = "14:00:00";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Create_WithTitleOf201Characters_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await PostAsync(
            claims,
            _fixture.CreateOrderBody(new string(c: 'a', WorkOrderApiTestData.TitleTooLongLength)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithEstimateOffTheGrid_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await PostAsync(
            claims,
            _fixture.CreateOrderBody(estimateMinutes: WorkOrderApiTestData.OffGridEstimate));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_With21Files_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await PostAsync(claims, _fixture.CreateOrderBody(fileCount: WorkOrderApiTestData.TooManyFiles));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithUnknownPriority_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await PostAsync(
            claims,
            _fixture.CreateOrderBody(priority: WorkOrderApiTestData.UnknownPriority));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithUnknownChecklistValue_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await PostAsync(
            claims,
            _fixture.CreateOrderBody(description: WorkOrderApiTestData.BadChecklist));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Get_WithMalformedId_Returns404()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            $"{WorkOrderApiTestData.BasePath}/{WorkOrderApiTestData.MalformedId}",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ChangeStatus_OfBacklogWorkOrder_Returns422WithProblemDetails()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());
        var order = await _fixture.ReadOrderAsync(claims, id);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            $"{WorkOrderApiTestData.BasePath}/{id}/status",
            claims,
            new JsonObject
            {
                [propertyName: "status"] = "inProgress",
                [propertyName: "rowVersion"] = order[propertyName: "rowVersion"]!.GetValue<uint>(),
            }.ToJsonString());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        problem![propertyName: "status"]!.GetValue<int>().Should().Be(expected: 422);
        problem[propertyName: "detail"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchBatch_WithNullEntry_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            WorkOrderApiTestData.SearchBatchPath,
            claims,
            new JsonObject { [propertyName: "searches"] = new JsonArray([null]) }.ToJsonString());

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var errorKeys = problem![propertyName: "errors"]?.AsObject().Select(error => error.Key).ToList() ?? [];

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errorKeys.Should().Equal("searches[0]");
    }

    [Fact]
    public async Task ChangeStatus_WithNullStatus_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());
        var order = await _fixture.ReadOrderAsync(claims, id);

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            $"{WorkOrderApiTestData.BasePath}/{id}/status",
            claims,
            new JsonObject
            {
                [propertyName: "status"] = null,
                [propertyName: "rowVersion"] = order[propertyName: "rowVersion"]!.GetValue<uint>(),
            }.ToJsonString());

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var errorKeys = problem![propertyName: "errors"]?.AsObject().Select(error => error.Key).ToList() ?? [];

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errorKeys.Should().Equal("status");
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409WithProblemDetails()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());
        var order = await _fixture.ReadOrderAsync(claims, id);
        var body = UpdateBody(order[propertyName: "rowVersion"]!.GetValue<uint>());
        var first = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put, $"{WorkOrderApiTestData.BasePath}/{id}", claims, body);

        var second = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put, $"{WorkOrderApiTestData.BasePath}/{id}", claims, body);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        problem![propertyName: "status"]!.GetValue<int>().Should().Be(expected: 409);
    }

    [Fact]
    public async Task OverrideThatOrphansAWorkOrder_Returns422WithEncryptedWorkOrderIds()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(WorkOrderApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(worker.EmployeeId);
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var start = _fixture.FutureStart(StartTime);
        var id = await _fixture.CreateOrderAsync(
            claims,
            _fixture.CreateOrderBody(encryptedAssigneeId: worker.EncryptedId, plannedStartLocal: start));
        var date = start[..start.IndexOf(value: 'T')];

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put,
            $"v1/employees/{worker.EncryptedId}/shift-overrides/{date}",
            claims,
            _fixture.CreateOverrideBody());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var listed = problem![propertyName: "workOrders"]!.AsArray();

        listed.Should().ContainSingle();
        listed[index: 0]![propertyName: "id"]!.GetValue<string>().Should().Be(id);
    }

    private static string UpdateBody(uint rowVersion) =>
        new JsonObject
        {
            [propertyName: "title"] = WorkOrderApiTestData.Title,
            [propertyName: "priority"] = "low",
            [propertyName: "category"] = "repair",
            [propertyName: "estimateMinutes"] = WorkOrderApiTestData.ValidEstimate,
            [propertyName: "fileIds"] = new JsonArray(),
            [propertyName: "rowVersion"] = rowVersion,
        }.ToJsonString();

    private Task<HttpResponseMessage> PostAsync(Testing.Authentication.TestTokenClaims claims, string body) =>
        _fixture.SendShiftRequestAsync(HttpMethod.Post, WorkOrderApiTestData.BasePath, claims, body);
}
