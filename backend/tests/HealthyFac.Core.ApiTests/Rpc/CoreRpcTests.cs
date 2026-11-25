using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using HealthyFac.Core.ApiTests.Hosting;
using HealthyFac.Core.ApiTests.Machines.Data;
using HealthyFac.Core.ApiTests.Rpc.Data;
using HealthyFac.Core.ApiTests.Shifts.Data;
using HealthyFac.Core.ApiTests.WorkOrders.Data;
using HealthyFac.Core.Contracts.Employees;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Core.Contracts.Schedules;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Testing.Authentication;

namespace HealthyFac.Core.ApiTests.Rpc;

[Collection(CoreApiCollection.Name)]
public sealed class CoreRpcTests(CoreApiFixture fixture) : IAsyncLifetime
{
    private readonly CoreApiFixture _fixture = fixture;
    private CoreGrpcHost _host = null!;

    public ValueTask InitializeAsync()
    {
        _host = _fixture.CreateGrpcHost();

        return ValueTask.CompletedTask;
    }

    public ValueTask DisposeAsync() => _host.DisposeAsync();

    [Fact]
    public async Task SearchMachines_WithANamePrefixAndAState_ReturnsTheMatchingMachinesOfTheCompany()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(CoreRpcTestData.MachineSerial));
        await _fixture.ArrangeMachineOfAnotherCompanyAsync();

        var reply = await _host.Machines.SearchMachinesAsync(
            new SearchMachinesRequest
            {
                SerialNumber = CoreRpcTestData.MachineSerial,
                States = { CoreRpcTestData.OperationalState },
                Limit = CoreRpcTestData.PageLimit,
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.TotalCount.Should().Be(expected: 1);
        reply.Machines.Single().Id.Should().Be(machineId.ToString());
        reply.Machines.Single().State.Should().Be(CoreRpcTestData.OperationalState);
    }

    [Fact]
    public async Task SearchMachines_WithOuterSpacesInTheSerialNumber_FindsTheMachineLikeOverHttp()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(CoreRpcTestData.MachineSerial));

        var reply = await _host.Machines.SearchMachinesAsync(
            new SearchMachinesRequest
            {
                SerialNumber = CoreRpcTestData.PaddedMachineSerial,
                Limit = CoreRpcTestData.PageLimit,
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Machines.Select(machine => machine.Id).Should().Equal(machineId.ToString());
    }

    [Fact]
    public async Task GetMachine_WithAMachineOfTheCompany_ReturnsItsFields()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(CoreRpcTestData.MachineSerial));

        var reply = await _host.Machines.GetMachineAsync(
            new GetMachineRequest { MachineId = machineId.ToString() },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.SerialNumber.Should().Be(CoreRpcTestData.MachineSerial);
        reply.ExpectedGoLiveOn.Should().Be(expected: "2099-01-01");
    }

    [Fact]
    public async Task GetMachine_WithAMachineOfAnotherCompany_ReturnsNotFound()
    {
        var otherId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineOfAnotherCompanyAsync());

        var act = async () => await _host.Machines.GetMachineAsync(
            new GetMachineRequest { MachineId = otherId.ToString() },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetMachineStateTimes_ForARangeAroundTheCreation_SplitsTheRangeIntoStatesAndTheTimeBefore()
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(CoreRpcTestData.MachineSerial));
        var to = DateTime.UtcNow.AddHours(value: 1);

        var reply = await _host.Machines.GetMachineStateTimesAsync(
            new GetMachineStateTimesRequest
            {
                MachineId = machineId.ToString(),
                From = Timestamp.FromDateTime(to.AddHours(-CoreRpcTestData.StateRangeHours)),
                To = Timestamp.FromDateTime(to),
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        (reply.BeforeCreationSeconds + reply.States.Sum(state => state.Seconds))
            .Should()
            .BeApproximately(CoreRpcTestData.StateRangeSeconds, precision: 1);
        reply.States.Should().Contain(state => state.State == CoreRpcTestData.OperationalState);
    }

    [Theory]
    [MemberData(nameof(CoreRpcTestData.MissingStateRangeBounds), MemberType = typeof(CoreRpcTestData))]
    public async Task GetMachineStateTimes_WithoutARangeBound_ReturnsInvalidArgument(
        string caseName,
        bool hasFrom,
        bool hasTo
    )
    {
        var machineId = _fixture.IdEncryptor.Decrypt(await _fixture.ArrangeMachineAsync(CoreRpcTestData.MachineSerial));
        var now = Timestamp.FromDateTime(DateTime.UtcNow);

        var act = async () => await _host.Machines.GetMachineStateTimesAsync(
            new GetMachineStateTimesRequest
            {
                MachineId = machineId.ToString(),
                From = hasFrom ? now : null,
                To = hasTo ? now : null,
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>(caseName);
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task SearchMachines_WithoutTheMachinesReadPermission_ReturnsPermissionDenied()
    {
        await _fixture.ArrangeMachineAsync(CoreRpcTestData.MachineSerial);

        var act = async () => await _host.Machines.SearchMachinesAsync(
            new SearchMachinesRequest { Limit = CoreRpcTestData.PageLimit },
            Headers(CoreRpcTestData.OtherPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task SearchWorkOrders_WithAWorkerToken_ReturnsOnlyTheOwnWorkOrders()
    {
        var arrangement = await ArrangeOrdersAsync();

        var supervisorReply = await _host.WorkOrders.SearchWorkOrdersAsync(
            new SearchWorkOrdersRequest { Limit = CoreRpcTestData.PageLimit },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);
        var workerReply = await _host.WorkOrders.SearchWorkOrdersAsync(
            new SearchWorkOrdersRequest { Limit = CoreRpcTestData.PageLimit },
            WorkerHeaders(arrangement.Worker.IdentityUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        supervisorReply.WorkOrders.Select(order => order.Title)
            .Should()
            .BeEquivalentTo(CoreRpcTestData.OwnOrderTitle, CoreRpcTestData.BacklogOrderTitle);
        workerReply.WorkOrders.Select(order => order.Title).Should().Equal(CoreRpcTestData.OwnOrderTitle);
        workerReply.WorkOrders.Single().AssigneeId.Should().Be(arrangement.Worker.EmployeeId.ToString());
        workerReply.WorkOrders.Single().PlannedEnd.Should().NotBeNull();
    }

    [Fact]
    public async Task SearchWorkOrders_WithTheCompanyReadPermissionAndNoManage_ReturnsEveryWorkOrder()
    {
        await ArrangeOrdersAsync();

        var reply = await _host.WorkOrders.SearchWorkOrdersAsync(
            new SearchWorkOrdersRequest { Limit = CoreRpcTestData.PageLimit },
            Headers(CoreRpcTestData.WorkOrdersRead, CoreRpcTestData.WorkOrdersReadCompany),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.WorkOrders.Select(order => order.Title)
            .Should()
            .BeEquivalentTo(CoreRpcTestData.OwnOrderTitle, CoreRpcTestData.BacklogOrderTitle);
    }

    [Fact]
    public async Task SearchWorkOrders_SortedByCreationNewestFirst_ReturnsTheNewestWorkOrderForALimitOfOne()
    {
        await ArrangeOrdersAsync();

        var reply = await _host.WorkOrders.SearchWorkOrdersAsync(
            new SearchWorkOrdersRequest
            {
                Limit = 1,
                SortColumn = CoreRpcTestData.CreatedAtColumn,
                SortDirection = CoreRpcTestData.DescendingDirection,
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.WorkOrders.Select(order => order.Title).Should().Equal(CoreRpcTestData.BacklogOrderTitle);
        reply.TotalCount.Should().Be(expected: 2);
    }

    [Fact]
    public async Task
        SearchWorkOrders_WithAServiceTokenAndTheCompanyReadPermission_ReturnsEveryWorkOrderOfTheActorCompany()
    {
        await ArrangeOrdersAsync();
        var token = _fixture.Tokens.CreateServiceToken(
            new TestServiceTokenClaims
            {
                Permissions = [CoreRpcTestData.WorkOrdersRead, CoreRpcTestData.WorkOrdersReadCompany],
            });

        var reply = await _host.WorkOrders.SearchWorkOrdersAsync(
            new SearchWorkOrdersRequest { Limit = CoreRpcTestData.PageLimit },
            CoreGrpcHost.BearerHeaders(token, Guid.CreateVersion7(), _fixture.CompanyId),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.WorkOrders.Select(order => order.Title)
            .Should()
            .BeEquivalentTo(CoreRpcTestData.OwnOrderTitle, CoreRpcTestData.BacklogOrderTitle);
    }

    [Fact]
    public async Task GetWorkOrder_ForAWorkOrderOutsideTheScopeOfAWorker_ReturnsNotFound()
    {
        var arrangement = await ArrangeOrdersAsync();

        var act = async () => await _host.WorkOrders.GetWorkOrderAsync(
            new GetWorkOrderRequest
            {
                WorkOrderId = _fixture.IdEncryptor.Decrypt(arrangement.BacklogOrderId).ToString(),
                HistoryLimit = CoreRpcTestData.HistoryLimit,
            },
            WorkerHeaders(arrangement.Worker.IdentityUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetWorkOrder_ForAnOwnWorkOrder_ReturnsTheFieldsAndTheHistory()
    {
        var arrangement = await ArrangeOrdersAsync();

        var reply = await _host.WorkOrders.GetWorkOrderAsync(
            new GetWorkOrderRequest
            {
                WorkOrderId = _fixture.IdEncryptor.Decrypt(arrangement.OwnOrderId).ToString(),
                HistoryLimit = CoreRpcTestData.HistoryLimit,
            },
            WorkerHeaders(arrangement.Worker.IdentityUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Title.Should().Be(CoreRpcTestData.OwnOrderTitle);
        reply.AssigneeId.Should().Be(arrangement.Worker.EmployeeId.ToString());
        reply.History.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SearchWorkOrders_WithoutTheWorkOrdersReadPermission_ReturnsPermissionDenied()
    {
        var act = async () => await _host.WorkOrders.SearchWorkOrdersAsync(
            new SearchWorkOrdersRequest { Limit = CoreRpcTestData.PageLimit },
            Headers(CoreRpcTestData.OtherPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task LookupEmployees_WithANamePart_ReturnsTheEmployeeOfTheCompany()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);

        var reply = await _host.Employees.LookupEmployeesAsync(
            new LookupEmployeesRequest { Search = ShiftApiTestData.FirstName },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Employees.Select(option => option.Id).Should().Equal(employee.EmployeeId.ToString());
    }

    [Fact]
    public async Task LookupEmployees_WithAnEmployeeId_ReturnsOnlyThatEmployee()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.OtherEmail);

        var reply = await _host.Employees.LookupEmployeesAsync(
            new LookupEmployeesRequest { EmployeeId = employee.EmployeeId.ToString() },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Employees.Select(option => option.Id).Should().Equal(employee.EmployeeId.ToString());
    }

    [Fact]
    public async Task LookupEmployees_WithAnEmployeeIdOfNoEmployee_ReturnsNoOption()
    {
        await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);

        var reply = await _host.Employees.LookupEmployeesAsync(
            new LookupEmployeesRequest { EmployeeId = Guid.CreateVersion7().ToString() },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Employees.Should().BeEmpty();
    }

    [Fact]
    public async Task LookupEmployees_WithOuterSpacesInTheSearch_FindsTheEmployeeLikeOverHttp()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);

        var reply = await _host.Employees.LookupEmployeesAsync(
            new LookupEmployeesRequest { Search = CoreRpcTestData.PaddedSearch },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Employees.Select(option => option.Id).Should().Equal(employee.EmployeeId.ToString());
    }

    [Fact]
    public async Task LookupEmployees_WithoutTheEmployeesReadPermission_ReturnsPermissionDenied()
    {
        var act = async () => await _host.Employees.LookupEmployeesAsync(
            new LookupEmployeesRequest(),
            Headers(CoreRpcTestData.OtherPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetEmployeeSchedule_WithoutTheShiftsReadPermission_ReturnsPermissionDenied()
    {
        var act = async () => await _host.Schedules.GetEmployeeScheduleAsync(
            new GetEmployeeScheduleRequest
            {
                EmployeeId = Guid.CreateVersion7().ToString(),
                Date = CoreRpcTestData.WeekdayDate,
            },
            Headers(CoreRpcTestData.OtherPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetFreeSpans_WithoutTheShiftsReadPermission_ReturnsPermissionDenied()
    {
        var act = async () => await _host.Schedules.GetFreeSpansAsync(
            new GetFreeSpansRequest
            {
                EmployeeId = Guid.CreateVersion7().ToString(),
                Date = CoreRpcTestData.WeekdayDate,
            },
            Headers(CoreRpcTestData.OtherPermission),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetFreeSpans_ForAnotherEmployeeWithAWorkerToken_ReturnsNotFound()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var other = await _fixture.ArrangeActiveEmployeeAsync(CoreRpcTestData.SecondWorkerEmail);

        var act = async () => await _host.Schedules.GetFreeSpansAsync(
            new GetFreeSpansRequest
            {
                EmployeeId = other.EmployeeId.ToString(),
                Date = CoreRpcTestData.WeekdayDate,
            },
            WorkerHeaders(worker.IdentityUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetEmployeeSchedule_ForTheOwnEmployee_ReturnsTheBlocksOfTheDay()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(employee.EmployeeId);

        var reply = await _host.Schedules.GetEmployeeScheduleAsync(
            new GetEmployeeScheduleRequest
            {
                EmployeeId = employee.EmployeeId.ToString(),
                Date = CoreRpcTestData.WeekdayDate,
            },
            WorkerHeaders(employee.IdentityUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Blocks.Should().HaveCount(CoreRpcTestData.BlockCount);
        reply.Blocks.First().StartLocal.Should().Be(expected: "08:00");
    }

    [Fact]
    public async Task GetEmployeeSchedule_ForAnotherEmployeeWithAWorkerToken_ReturnsNotFound()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        var other = await _fixture.ArrangeActiveEmployeeAsync(CoreRpcTestData.SecondWorkerEmail);

        var act = async () => await _host.Schedules.GetEmployeeScheduleAsync(
            new GetEmployeeScheduleRequest
            {
                EmployeeId = other.EmployeeId.ToString(),
                Date = CoreRpcTestData.WeekdayDate,
            },
            WorkerHeaders(worker.IdentityUserId),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.NotFound);
    }

    [Fact]
    public async Task GetEmployeeSchedule_WithAMalformedDate_ReturnsInvalidArgument()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);

        var act = async () => await _host.Schedules.GetEmployeeScheduleAsync(
            new GetEmployeeScheduleRequest
            {
                EmployeeId = employee.EmployeeId.ToString(),
                Date = CoreRpcTestData.MalformedDate,
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.InvalidArgument);
    }

    [Fact]
    public async Task GetCompanySchedule_WithASupervisorToken_ReturnsTheDayOfEveryEmployee()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(employee.EmployeeId);

        var reply = await _host.Schedules.GetCompanyScheduleAsync(
            new GetCompanyScheduleRequest { Date = CoreRpcTestData.WeekdayDate },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        var day = reply.Employees.Single();
        day.EmployeeId.Should().Be(employee.EmployeeId.ToString());
        day.Day.Blocks.Should().HaveCount(CoreRpcTestData.BlockCount);
    }

    [Fact]
    public async Task GetCompanySchedule_WithoutTheShiftsReadCompanyPermission_ReturnsPermissionDenied()
    {
        var act = async () => await _host.Schedules.GetCompanyScheduleAsync(
            new GetCompanyScheduleRequest { Date = CoreRpcTestData.WeekdayDate },
            Headers(CoreRpcTestData.ShiftsRead, CoreRpcTestData.EmployeesRead),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetCompanySchedule_WithoutTheEmployeesReadPermission_ReturnsPermissionDenied()
    {
        var act = async () => await _host.Schedules.GetCompanyScheduleAsync(
            new GetCompanyScheduleRequest { Date = CoreRpcTestData.WeekdayDate },
            Headers(CoreRpcTestData.ShiftsReadCompany),
            cancellationToken: TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<RpcException>();
        exception.Which.StatusCode.Should().Be(StatusCode.PermissionDenied);
    }

    [Fact]
    public async Task GetFreeSpans_ForAnEmployeeWithBlocks_ReturnsTheSpansOfTheDay()
    {
        var employee = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(employee.EmployeeId);

        var reply = await _host.Schedules.GetFreeSpansAsync(
            new GetFreeSpansRequest
            {
                EmployeeId = employee.EmployeeId.ToString(),
                Date = CoreRpcTestData.WeekdayDate,
            },
            SupervisorHeaders(),
            cancellationToken: TestContext.Current.CancellationToken);

        reply.Spans.Should().HaveCount(CoreRpcTestData.BlockCount);
        reply.Spans.First().StartLocal.Should().EndWith(expected: "08:00");
    }

    private async Task<OrderArrangement> ArrangeOrdersAsync()
    {
        var worker = await _fixture.ArrangeActiveEmployeeAsync(ShiftApiTestData.WorkerEmail);
        await _fixture.SeedWorkWeekAsync(worker.EmployeeId);
        var supervisor = _fixture.CreateSupervisorOrderClaims(Guid.CreateVersion7());
        var ownId = await _fixture.CreateOrderAsync(
            supervisor,
            _fixture.CreateOrderBody(
                CoreRpcTestData.OwnOrderTitle,
                encryptedAssigneeId: worker.EncryptedId,
                plannedStartLocal: _fixture.FutureStart(WorkOrderApiTestData.StartTime)));
        var backlogId = await _fixture.CreateOrderAsync(
            supervisor,
            _fixture.CreateOrderBody(CoreRpcTestData.BacklogOrderTitle));

        return new OrderArrangement(worker, ownId, backlogId);
    }

    private Metadata SupervisorHeaders() => Headers(CoreRpcTestData.SupervisorPermissions);

    private Metadata WorkerHeaders(Guid identityUserId) =>
        Bearer(
            _fixture.CreateShiftClaims(
                identityUserId,
                CoreRpcTestData.WorkOrdersRead,
                CoreRpcTestData.ShiftsRead,
                CoreRpcTestData.EmployeesRead));

    private Metadata Headers(params string[] permissions) =>
        Bearer(_fixture.CreateMachineClaims(permissions));

    private Metadata Bearer(TestTokenClaims claims) => CoreGrpcHost.BearerHeaders(_fixture.Tokens.CreateToken(claims));

    private sealed record OrderArrangement(ShiftApiEmployee Worker, string OwnOrderId, string BacklogOrderId);
}
