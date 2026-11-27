using System.Net;
using System.Text.Json.Nodes;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;
using HealthyFac.Core.Web.Authorization;
using HealthyFac.Testing.Features;

namespace HealthyFac.Core.ApiTests.WorkOrders;

[Collection(CoreApiCollection.Name)]
[Feature(TestFeatures.WorkOrders)]
public sealed class WorkOrderAuthorizationTests(CoreApiFixture fixture)
{
    private const string StartTime = "09:00:00";
    private const string FreeStartTime = "11:00:00";

    private readonly CoreApiFixture _fixture = fixture;

    [Fact]
    public async Task Search_WithWorkerToken_ReturnsOnlyOwnRows()
    {
        var arranged = await ArrangeAsync();

        var workerIds = await _fixture.SearchTitlesAsync(arranged.WorkerClaims);
        var supervisorIds = await _fixture.SearchTitlesAsync(arranged.SupervisorClaims);

        workerIds.Should().BeEquivalentTo(arranged.AssignedToWorkerId, arranged.WorkerBacklogId);

        supervisorIds.Should()
            .BeEquivalentTo(
                arranged.AssignedToWorkerId,
                arranged.AssignedToOtherId,
                arranged.SupervisorBacklogId,
                arranged.WorkerBacklogId);
    }

    [Fact]
    public async Task Lookup_WithWorkerAndCompanyReadTokens_ReturnsOwnRowsAndEveryRow()
    {
        var arranged = await ArrangeAsync();

        var readerClaims = arranged.WorkerClaims with
        {
            Permissions = [WorkOrderApiTestData.ReadPermission, WorkOrderApiTestData.ReadCompanyPermission],
        };

        const string body = """{"search":"replace"}""";

        var worker = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.LookupPath, arranged.WorkerClaims, body);

        var reader = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.LookupPath, readerClaims, body);

        var workerText = await worker.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var readerText = await reader.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var workerCount = JsonNode.Parse(workerText)!["items"]!.AsArray().Count;
        var readerCount = JsonNode.Parse(readerText)!["items"]!.AsArray().Count;
        workerCount.Should().Be(expected: 2);
        readerCount.Should().Be(expected: 4);
    }

    [Fact]
    public async Task Search_WithCompanyReadTokenWithoutManage_ReturnsEveryRow()
    {
        var arranged = await ArrangeAsync();

        var claims = arranged.WorkerClaims with
        {
            Permissions = [WorkOrderApiTestData.ReadPermission, WorkOrderApiTestData.ReadCompanyPermission],
        };

        var ids = await _fixture.SearchTitlesAsync(claims);

        ids.Should()
            .BeEquivalentTo(
                arranged.AssignedToWorkerId,
                arranged.AssignedToOtherId,
                arranged.SupervisorBacklogId,
                arranged.WorkerBacklogId);
    }

    [Theory]
    [MemberData(nameof(WorkOrderApiTestData.WorkerWriteRoutes), MemberType = typeof(WorkOrderApiTestData))]
    public async Task Writes_WithCompanyReadTokenWithoutManage_Return403(
        string caseName,
        string method,
        string pathTemplate
    )
    {
        var arranged = await ArrangeAsync();

        var claims = arranged.WorkerClaims with
        {
            Permissions = [WorkOrderApiTestData.ReadPermission, WorkOrderApiTestData.ReadCompanyPermission],
        };

        var response = await _fixture.SendShiftRequestAsync(
            new HttpMethod(method),
            pathTemplate.Replace(oldValue: "{id}", arranged.AssignedToOtherId),
            claims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Fact]
    public async Task Get_WithWorkerTokenForOtherWorkOrder_Returns404AndForOwnReturns200()
    {
        var arranged = await ArrangeAsync();

        var other = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            $"{WorkOrderApiTestData.BasePath}/{arranged.AssignedToOtherId}",
            arranged.WorkerClaims);

        var own = await _fixture.SendShiftRequestAsync(
            HttpMethod.Get,
            $"{WorkOrderApiTestData.BasePath}/{arranged.AssignedToWorkerId}",
            arranged.WorkerClaims);

        other.StatusCode.Should().Be(HttpStatusCode.NotFound);
        own.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [MemberData(nameof(WorkOrderApiTestData.WorkerWriteRoutes), MemberType = typeof(WorkOrderApiTestData))]
    public async Task Writes_WithWorkerToken_Return403(string caseName, string method, string pathTemplate)
    {
        var arranged = await ArrangeAsync();

        var response = await _fixture.SendShiftRequestAsync(
            new HttpMethod(method),
            pathTemplate.Replace(oldValue: "{id}", arranged.AssignedToWorkerId.ToString()),
            arranged.WorkerClaims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, caseName);
    }

    [Fact]
    public async Task ChangeStatus_WithCompanyReadTokenWithoutManage_Returns404AndWithManageReturns204()
    {
        var arranged = await ArrangeAsync();

        var readerClaims = arranged.WorkerClaims with
        {
            Permissions =
            [
                WorkOrderApiTestData.ReadPermission,
                WorkOrderApiTestData.WritePermission,
                WorkOrderApiTestData.ReadCompanyPermission,
            ],
        };

        var order = await _fixture.ReadOrderAsync(arranged.SupervisorClaims, arranged.AssignedToOtherId);

        var body = new JsonObject
        {
            ["status"] = "rejected",
            ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
        }.ToJsonString();

        var path = $"{WorkOrderApiTestData.BasePath}/{arranged.AssignedToOtherId}/status";

        var reader = await _fixture.SendShiftRequestAsync(HttpMethod.Post, path, readerClaims, body);
        var supervisor = await _fixture.SendShiftRequestAsync(HttpMethod.Post, path, arranged.SupervisorClaims, body);

        reader.StatusCode.Should().Be(HttpStatusCode.NotFound);
        supervisor.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_WithCompanyReadTokenWithoutManage_Returns404AndWithManageReturns204()
    {
        var arranged = await ArrangeAsync();

        var readerClaims = arranged.WorkerClaims with
        {
            Permissions =
            [
                WorkOrderApiTestData.ReadPermission,
                WorkOrderApiTestData.WritePermission,
                WorkOrderApiTestData.ReadCompanyPermission,
            ],
        };

        var order = await _fixture.ReadOrderAsync(arranged.SupervisorClaims, arranged.SupervisorBacklogId);

        var body = new JsonObject
        {
            ["title"] = WorkOrderApiTestData.Title,
            ["priority"] = "low",
            ["category"] = "repair",
            ["estimateMinutes"] = WorkOrderApiTestData.ValidEstimate,
            ["fileIds"] = new JsonArray(),
            ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
        }.ToJsonString();

        var path = $"{WorkOrderApiTestData.BasePath}/{arranged.SupervisorBacklogId}";

        var reader = await _fixture.SendShiftRequestAsync(HttpMethod.Put, path, readerClaims, body);
        var supervisor = await _fixture.SendShiftRequestAsync(HttpMethod.Put, path, arranged.SupervisorClaims, body);

        reader.StatusCode.Should().Be(HttpStatusCode.NotFound);
        supervisor.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_WithCompanyReadTokenWithoutWriteCompany_Returns403AndWithItReturns201()
    {
        var arranged = await ArrangeAsync();

        var readerClaims = arranged.WorkerClaims with
        {
            Permissions =
            [
                WorkOrderApiTestData.ReadPermission,
                WorkOrderApiTestData.WritePermission,
                WorkOrderApiTestData.ReadCompanyPermission,
            ],
        };

        var body = _fixture.CreateOrderBody(
            encryptedAssigneeId: arranged.WorkerEncryptedId,
            plannedStartLocal: _fixture.FutureStart(FreeStartTime));

        var reader = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.BasePath, readerClaims, body);

        var supervisor = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.BasePath, arranged.SupervisorClaims, body);

        reader.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        supervisor.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ApplyDraft_WithCompanyReadTokenWithoutWriteCompany_Returns403AndWithItReturns200()
    {
        var arranged = await ArrangeAsync();

        var readerClaims = arranged.WorkerClaims with
        {
            Permissions =
            [
                WorkOrderDraftApiTestData.DraftsWrite,
                WorkOrderApiTestData.ReadPermission,
                WorkOrderApiTestData.WritePermission,
                WorkOrderApiTestData.ReadCompanyPermission,
            ],
        };

        var supervisorClaims = arranged.SupervisorClaims with
        {
            Permissions = [.. arranged.SupervisorClaims.Permissions, WorkOrderDraftApiTestData.DraftsWrite],
        };

        var body = _fixture.CreateApplyBody(
            encryptedAssigneeId: arranged.WorkerEncryptedId,
            plannedStartLocal: _fixture.FutureStart(FreeStartTime));

        var readerDraftId = await _fixture.ArrangeWorkOrderDraftAsync();
        var supervisorDraftId = await _fixture.ArrangeWorkOrderDraftAsync();

        var reader = await _fixture.PostDraftAsync(readerClaims, readerDraftId, action: "apply", body);
        var supervisor = await _fixture.PostDraftAsync(supervisorClaims, supervisorDraftId, action: "apply", body);

        reader.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        supervisor.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithWorkerTokenAndAssignee_Returns403()
    {
        var arranged = await ArrangeAsync();

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post,
            WorkOrderApiTestData.BasePath,
            arranged.WorkerClaims,
            _fixture.CreateOrderBody(
                encryptedAssigneeId: arranged.WorkerEncryptedId,
                plannedStartLocal: _fixture.FutureStart(StartTime)));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithWorkerTokenAndAnotherStart_Returns403()
    {
        var arranged = await ArrangeAsync();
        var order = await _fixture.ReadOrderAsync(arranged.WorkerClaims, arranged.AssignedToWorkerId);

        var body = new JsonObject
        {
            ["title"] = WorkOrderApiTestData.Title,
            ["priority"] = "low",
            ["category"] = "repair",
            ["estimateMinutes"] = WorkOrderApiTestData.ValidEstimate,
            ["fileIds"] = new JsonArray(),
            ["assigneeId"] = arranged.WorkerEncryptedId,
            ["plannedStartLocal"] = _fixture.FutureStart(time: "10:00:00"),
            ["rowVersion"] = order["rowVersion"]!.GetValue<uint>(),
        }.ToJsonString();

        var response = await _fixture.SendShiftRequestAsync(
            HttpMethod.Put,
            $"{WorkOrderApiTestData.BasePath}/{arranged.AssignedToWorkerId}",
            arranged.WorkerClaims,
            body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SearchBatch_WithoutReadPermission_Returns403AndWithReadReturns200()
    {
        var claims = _fixture.CreateShiftClaims(Guid.CreateVersion7(), CorePermissions.Machines.Read);
        var readClaims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        const string body = """{"searches":[{"offset":0,"limit":10,"filter":{}}]}""";

        var forbidden = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.SearchBatchPath, claims, body);

        var allowed = await _fixture.SendShiftRequestAsync(
            HttpMethod.Post, WorkOrderApiTestData.SearchBatchPath, readClaims, body);

        forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        allowed.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<ArrangedOrders> ArrangeAsync()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(WorkOrderApiTestData.WorkerEmail);
        var other = await _fixture.ArrangeActiveEmployeeAsync(WorkOrderApiTestData.OtherEmail);
        await _fixture.SeedWorkWeekAsync(worker.EmployeeId);
        await _fixture.SeedWorkWeekAsync(other.EmployeeId);
        var workerClaims = _fixture.CreateWorkerClaims(worker.IdentityUserId);
        var supervisorClaims = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());

        return new ArrangedOrders
        {
            WorkerClaims = workerClaims,
            SupervisorClaims = supervisorClaims,
            WorkerEncryptedId = worker.EncryptedId,
            AssignedToWorkerId = await _fixture.CreateOrderAsync(
                supervisorClaims,
                _fixture.CreateOrderBody(
                    encryptedAssigneeId: worker.EncryptedId,
                    plannedStartLocal: _fixture.FutureStart(StartTime))),
            AssignedToOtherId = await _fixture.CreateOrderAsync(
                supervisorClaims,
                _fixture.CreateOrderBody(
                    encryptedAssigneeId: other.EncryptedId,
                    plannedStartLocal: _fixture.FutureStart(StartTime))),
            SupervisorBacklogId = await _fixture.CreateOrderAsync(supervisorClaims, _fixture.CreateOrderBody()),
            WorkerBacklogId = await _fixture.CreateOrderAsync(workerClaims, _fixture.CreateOrderBody()),
        };
    }

    private sealed record ArrangedOrders
    {
        public required Testing.Authentication.TestTokenClaims WorkerClaims { get; init; }

        public required Testing.Authentication.TestTokenClaims SupervisorClaims { get; init; }

        public required string WorkerEncryptedId { get; init; }

        public required string AssignedToWorkerId { get; init; }

        public required string AssignedToOtherId { get; init; }

        public required string SupervisorBacklogId { get; init; }

        public required string WorkerBacklogId { get; init; }
    }
}
