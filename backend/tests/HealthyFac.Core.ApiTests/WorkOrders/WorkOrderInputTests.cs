using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.ApiTests.WorkOrders;

[Collection(CoreApiCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
public sealed class WorkOrderInputTests(CoreApiFixture fixture)
{
    private const string StartTime = "14:00:00";

    private readonly CoreApiFixture _fixture = fixture;

    [Theory]
    [MemberData(nameof(WorkOrderApiTestData.InvalidCreateBodies), MemberType = typeof(WorkOrderApiTestData))]
    public async Task Create_WithInvalidBody_Returns400(string caseName, Func<CoreApiFixture, string> createBody)
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await PostAsync(claims, createBody(_fixture));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
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
                ["status"] = "inProgress",
                ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
            }.ToJsonString());

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        problem!["status"]!.GetValue<int>().Should().Be(expected: 422);
        problem["detail"]!.GetValue<string>().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchBatch_WithNullEntry_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            WorkOrderApiTestData.SearchBatchPath,
            claims,
            new JsonObject { ["searches"] = new JsonArray { null } }.ToJsonString());

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var errorKeys = problem!["errors"]?.AsObject().Select(error => error.Key).ToList() ?? [];

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
                ["status"] = null,
                ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
            }.ToJsonString());

        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var errorKeys = problem!["errors"]?.AsObject().Select(error => error.Key).ToList() ?? [];

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        errorKeys.Should().Equal("status");
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409WithProblemDetails()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());
        var order = await _fixture.ReadOrderAsync(claims, id);
        var body = UpdateBody(order["rowVersion"]!.GetValue<uint>());

        var first = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put, $"{WorkOrderApiTestData.BasePath}/{id}", claims, body);

        var second = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put, $"{WorkOrderApiTestData.BasePath}/{id}", claims, body);

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        problem!["status"]!.GetValue<int>().Should().Be(expected: 409);
    }

    [Fact]
    public async Task Update_WithAnAssigneeWithoutAStart_Returns400()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(WorkOrderApiTestData.WorkerEmail);
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());
        var order = await _fixture.ReadOrderAsync(claims, id);

        var body = new JsonObject
        {
            ["title"] = WorkOrderApiTestData.Title,
            ["priority"] = "low",
            ["category"] = "repair",
            ["estimateMinutes"] = WorkOrderApiTestData.ValidEstimate,
            ["fileIds"] = new JsonArray(),
            ["assigneeId"] = worker.EncryptedId,
            ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
        }.ToJsonString();

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put, $"{WorkOrderApiTestData.BasePath}/{id}", claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        var listed = problem!["workOrders"]!.AsArray();

        listed.Should().ContainSingle();
        listed[0]!["id"]!.GetValue<string>().Should().Be(id);
    }

    [Fact]
    public async Task History_WithoutALimit_Returns400()
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            $"{WorkOrderApiTestData.BasePath}/{id}/history",
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [MemberData(nameof(WorkOrderApiTestData.InvalidApplyBodies), MemberType = typeof(WorkOrderApiTestData))]
    public async Task ApplySchedule_WithAnInvalidBody_Returns400(string caseName, string bodyTemplate)
    {
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.ApplyPath, claims, bodyTemplate.Replace(oldValue: "{id}", id));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, caseName);
    }

    [Fact]
    public async Task ApplySchedule_WithASlotInThePast_Returns422WithEncryptedFailureIds()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(WorkOrderApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(worker.EmployeeId);
        var claims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var id = await _fixture.CreateOrderAsync(claims, _fixture.CreateOrderBody());
        var order = await _fixture.ReadOrderAsync(claims, id);

        var body = new JsonObject
        {
            ["changes"] = new JsonArray(
                new JsonObject
                {
                    ["workOrderId"] = id,
                    ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
                    ["assigneeId"] = worker.EncryptedId,
                    ["plannedStartLocal"] = WorkOrderApiTestData.PastStart,
                    ["estimateMinutes"] = WorkOrderApiTestData.ValidEstimate,
                }),
        }.ToJsonString();

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.ApplyPath, claims, body);

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        var problem = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);
        var failures = problem!["changes"]!.AsArray();

        failures.Should().ContainSingle();
        failures[0]!["workOrderId"]!.GetValue<string>().Should().Be(id);
        failures[0]!["reason"]!.GetValue<string>().Should().Be(expected: "startOutOfRange");
    }

    private static string UpdateBody(uint rowVersion) =>
        new JsonObject
        {
            ["title"] = WorkOrderApiTestData.Title,
            ["priority"] = "low",
            ["category"] = "repair",
            ["estimateMinutes"] = WorkOrderApiTestData.ValidEstimate,
            ["fileIds"] = new JsonArray(),
            ["rowVersion"] = rowVersion,
        }.ToJsonString();

    private Task<HttpResponseMessage> PostAsync(Testing.Authentication.TestTokenClaims claims, string body) =>
        _fixture.SendShiftRequestAsync(HttpMethod.Post, WorkOrderApiTestData.BasePath, claims, body);
}
