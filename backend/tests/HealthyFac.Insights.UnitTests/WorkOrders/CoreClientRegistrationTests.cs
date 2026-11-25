using Grpc.Core;
using HealthyFac.Core.Contracts.WorkOrders;
using HealthyFac.Insights.UnitTests.WorkOrders.Data;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Testing.Features;

namespace HealthyFac.Insights.UnitTests.WorkOrders;

[Feature(TestFeatures.Cases)]
[Feature(TestFeatures.WorkOrders)]
public sealed class CoreClientRegistrationTests
{
    [Fact]
    public async Task CoreWorkerClient_WithAFlowActor_SendsTheServiceTokenAndTheActorHeaders()
    {
        using var harness = new CoreClientRegistrationHarness();
        var actor = ServiceActor.InboxProcessor.For(ServiceName.Insights, CoreClientRegistrationTestData.CompanyId);
        using var actorScope = new ActorContextHolder().Enter(actor);

        var act = async () => await harness.CreateClient()
            .SearchWorkOrdersAsync(
                new SearchWorkOrdersRequest(),
                cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<RpcException>();
        var request = harness.Core.Request!;
        request.Headers.Authorization!.Parameter.Should().Be(CoreClientRegistrationTestData.ServiceToken);
        request.Headers.GetValues(ActorHeaderNames.ActorId).Should().Equal(actor.UserId.ToString());

        request.Headers.GetValues(ActorHeaderNames.CompanyId)
            .Should()
            .Equal(CoreClientRegistrationTestData.CompanyId.ToString());
    }
}
