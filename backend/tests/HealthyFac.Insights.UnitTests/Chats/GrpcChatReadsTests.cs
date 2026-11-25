using Grpc.Core;
using HealthyFac.Core.Contracts.Machines;
using HealthyFac.Insights.Application.Chats.Tools;
using HealthyFac.Insights.Application.Chats.Tools.Reads;
using HealthyFac.Insights.Application.Ingest.Processing.Failures;
using HealthyFac.Insights.Infrastructure.Chats.Reads;
using HealthyFac.Insights.UnitTests.Chats.Data;
using HealthyFac.Insights.UnitTests.Files.Data;
using HealthyFac.Shared.Application.DownstreamScopes;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Telemetry.Contracts.Definitions;

namespace HealthyFac.Insights.UnitTests.Chats;

public sealed class GrpcChatReadsTests
{
    private const string ReplyBody = "secret reply body of the service";

    private readonly FakeDocumentCallInvoker _invoker = new(new DownstreamScopeHolder());
    private readonly GrpcChatReads _reads;

    public GrpcChatReadsTests()
    {
        _reads = new GrpcChatReads(new FakeChatClientFactory(_invoker));
    }

    [Theory]
    [InlineData(StatusCode.NotFound)]
    [InlineData(StatusCode.PermissionDenied)]
    public async Task GetWorkOrder_WhenTheServiceDeniesOrDoesNotFindIt_ThrowsTheFixedTextWithoutTheReplyBody(
        StatusCode code
    )
    {
        _invoker.Respond = _ => throw new RpcException(new Status(code, ReplyBody));

        var act = async () => await _reads.GetWorkOrderAsync(
            new GetWorkOrderArguments { WorkOrderId = Guid.CreateVersion7() },
            TestContext.Current.CancellationToken);

        var exception = await act.Should().ThrowAsync<NotFoundException>();
        exception.Which.Message.Should().Be(ChatToolErrors.NotFoundOrNotAllowed);
        exception.Which.ToString().Should().NotContain(ReplyBody);
    }

    [Fact]
    public async Task SearchMachines_WhenTheServiceIsDown_ThrowsAProviderUnavailableException()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.Unavailable, ReplyBody));

        var act = async () => await _reads.SearchMachinesAsync(
            new SearchMachinesArguments(),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<ProviderUnavailableException>();
    }

    [Fact]
    public async Task SearchMachines_WhenTheServiceRejectsTheArguments_KeepsTheRpcException()
    {
        _invoker.Respond = _ => throw new RpcException(new Status(StatusCode.InvalidArgument, ReplyBody));

        var act = async () => await _reads.SearchMachinesAsync(
            new SearchMachinesArguments(),
            TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RpcException>();
    }

    [Fact]
    public async Task SearchMachines_WithArguments_SendsThemUnchangedAndMapsTheRows()
    {
        var machineId = Guid.CreateVersion7();
        SearchMachinesRequest? sent = null;
        _invoker.Respond = request =>
        {
            sent = (SearchMachinesRequest)request;

            return new SearchMachinesReply
            {
                TotalCount = 1,
                Machines = { new MachineReply { Id = machineId.ToString(), Name = "Press", State = "operational" } },
            };
        };

        var response = await _reads.SearchMachinesAsync(
            new SearchMachinesArguments { Name = "Pre", States = ["operational"], Limit = 5, Offset = 10 },
            TestContext.Current.CancellationToken);

        sent!.Name.Should().Be(expected: "Pre");
        sent.States.Should().Equal("operational");
        sent.Limit.Should().Be(expected: 5);
        sent.Offset.Should().Be(expected: 10);
        response.TotalCount.Should().Be(expected: 1);
        response.Machines.Single().Id.Should().Be(machineId);
        response.Machines.Single().Description.Should().BeNull();
    }

    [Fact]
    public async Task GetMachine_WithDefinitions_JoinsTheMachineTheMetricIdsAndTheStatuses()
    {
        var machineId = Guid.CreateVersion7();
        var metricId = Guid.CreateVersion7();
        _invoker.Respond = request => request switch
        {
            GetMachineRequest => new MachineReply { Id = machineId.ToString(), Description = "Main press" },
            _ => new ListDefinitionsReply
            {
                Metrics = { new MetricDefinition { Id = metricId.ToString(), Key = "temp", Min = -5 } },
                Statuses =
                {
                    new StatusDefinition { Id = Guid.CreateVersion7().ToString(), Code = "running", Severity = "info" },
                },
            },
        };

        var response = await _reads.GetMachineAsync(
            new GetMachineArguments { MachineId = machineId },
            TestContext.Current.CancellationToken);

        response.Machine.Description.Should().Be(expected: "Main press");
        var metric = response.Metrics.Should().ContainSingle().Subject;
        metric.Id.Should().Be(metricId);
        metric.Min.Should().Be(expected: -5);
        metric.Max.Should().BeNull();
        response.Statuses.Single().Code.Should().Be(expected: "running");
    }
}
