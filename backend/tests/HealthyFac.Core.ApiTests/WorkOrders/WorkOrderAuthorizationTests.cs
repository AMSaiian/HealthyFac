using System.Net;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;
using HealthyFac.Core.Web.Authorization;

namespace HealthyFac.Core.ApiTests.WorkOrders;

[Collection(CoreApiCollection.Name)]
public sealed class WorkOrderAuthorizationTests(CoreApiFixture fixture)
{
    private const string StartTime = "09:00:00";

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
            pathTemplate.Replace("{id}", arranged.AssignedToWorkerId.ToString()),
            arranged.WorkerClaims);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, because: caseName);
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
