using Grpc.Core;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.WorkOrders;
using HealthyFac.Insights.UnitTests.Files.Data;
using HealthyFac.Insights.UnitTests.WorkOrders.Data;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.WorkOrders;

[Feature(TestFeatures.Cases)]
[Feature(TestFeatures.WorkOrders)]
public sealed class CoreWorkOrderCatalogTests
{
    private readonly DownstreamScopeHolder _holder = new();
    private readonly FakeDocumentCallInvoker _invoker;
    private readonly CoreWorkOrderCatalog _catalog;

    public CoreWorkOrderCatalogTests()
    {
        _invoker = new FakeDocumentCallInvoker(_holder);
        _catalog = new CoreWorkOrderCatalog(new FakeCoreClientFactory(_invoker), _holder);
    }

    [Fact]
    public async Task ListOpen_SendsTheOpenStatusesTheMachineTheLimitAndTheNewestFirstSortInTheWorkOrdersScope()
    {
        SearchWorkOrdersRequest? sent = null;

        _invoker.Respond = request =>
        {
            sent = (SearchWorkOrdersRequest)request;

            return CoreWorkOrderCatalogTestData.CreateReply(hasPlannedTimes: true);
        };

        await _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            TestContext.Current.CancellationToken);

        sent!.Statuses.Should().Equal(CoreWorkOrderCatalogTestData.OpenStatuses);
        sent.MachineId.Should().Be(CoreWorkOrderCatalogTestData.MachineId.ToString());
        sent.Limit.Should().Be(CoreWorkOrderCatalogTestData.Limit);
        sent.SortColumn.Should().Be(CoreWorkOrderCatalogTestData.CreatedAtColumn);
        sent.SortDirection.Should().Be(CoreWorkOrderCatalogTestData.DescendingDirection);

        _invoker.ScopesAtCall.Should()
            .ContainSingle()
            .Which.Should()
            .Equal(CoreWorkOrderCatalogTestData.DownstreamScope);
    }

    [Fact]
    public async Task ListOpen_WithAPlannedWorkOrder_MapsTheFieldsWithoutTheAssignee()
    {
        _invoker.Respond = _ => CoreWorkOrderCatalogTestData.CreateReply(hasPlannedTimes: true);

        var orders = await _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            TestContext.Current.CancellationToken);

        var order = orders.Should().ContainSingle().Subject;
        order.Id.Should().Be(CoreWorkOrderCatalogTestData.WorkOrderId);
        order.Title.Should().Be(expected: "Replace the filter");
        order.Status.Should().Be(expected: "staged");
        order.PlannedStartUtc.Should().Be(CoreWorkOrderCatalogTestData.PlannedStart);
        order.PlannedEndUtc.Should().Be(CoreWorkOrderCatalogTestData.PlannedStart.AddHours(value: 1));
    }

    [Fact]
    public async Task ListOpen_WithAnUnplannedWorkOrder_LeavesTheTimesEmpty()
    {
        _invoker.Respond = _ => CoreWorkOrderCatalogTestData.CreateReply(hasPlannedTimes: false);

        var orders = await _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            TestContext.Current.CancellationToken);

        var order = orders.Should().ContainSingle().Subject;
        order.PlannedStartUtc.Should().BeNull();
        order.PlannedEndUtc.Should().BeNull();
    }

    [Theory]
    [MemberData(
        nameof(CoreWorkOrderCatalogTestData.OutageStatuses),
        MemberType = typeof(CoreWorkOrderCatalogTestData))]
    public async Task ListOpen_WhenCoreIsDown_ThrowsProviderUnavailableAndLeavesTheDownstreamScope(
        string caseName,
        StatusCode status
    )
    {
        _invoker.Respond = _ => throw new RpcException(new Status(status, detail: "down"));

        var act = () => _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>(caseName);
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task ListOpen_WhenTheCallerCancels_ThrowsOperationCanceledAndLeavesTheDownstreamScope()
    {
        using var source = new CancellationTokenSource();

        _invoker.Respond = _ =>
        {
            source.Cancel();

            throw new RpcException(new Status(StatusCode.Cancelled, detail: "cancelled"));
        };

        var act = () => _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            source.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        _holder.Current.Should().BeEmpty();
    }

    [Fact]
    public async Task ListOpen_WhenCoreCancelsWithoutTheCaller_ThrowsTheRpcException()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.Cancelled, detail: "cancelled"));

        var act = () => _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RpcException>();
    }

    [Fact]
    public async Task ListOpen_WhenCoreAnswersNotFound_ThrowsTheRpcException()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.NotFound, detail: "missing"));

        var act = () => _catalog.ListOpenAsync(
            CoreWorkOrderCatalogTestData.MachineId,
            CoreWorkOrderCatalogTestData.Limit,
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RpcException>();
    }
}
