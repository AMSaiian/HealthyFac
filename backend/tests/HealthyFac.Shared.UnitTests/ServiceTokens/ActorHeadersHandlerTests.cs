using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.Infrastructure.ServiceTokens;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Shared.UnitTests.ServiceTokens.Data;

namespace HealthyFac.Shared.UnitTests.ServiceTokens;

public sealed class ActorHeadersHandlerTests
{
    private readonly ActorContextHolder _holder = new();

    [Fact]
    public async Task ActorHeaders_WithServiceActor_SetsActorAndCompanyHeaders()
    {
        var actor = ServiceActor.OutboxProcessor.For(ServiceName.Core, ActorTestData.CompanyId);
        using var actorScope = _holder.Enter(actor);
        using var harness = new ActorHeadersHarness();

        using var response = await harness.CallAsync();

        harness.Resource.ActorHeaders.Should().Equal((actor.UserId.ToString(), ActorTestData.CompanyId.ToString()));
    }

    [Fact]
    public async Task ActorHeaders_WithActorWithoutCompany_SendsNoCompanyHeader()
    {
        var actor = ServiceActor.OutboxProcessor.For(ServiceName.Core);
        using var actorScope = _holder.Enter(actor);
        using var harness = new ActorHeadersHarness();

        using var response = await harness.CallAsync();

        harness.Resource.ActorHeaders.Should().Equal((actor.UserId.ToString(), null));
    }

    [Fact]
    public async Task ActorHeaders_InInboxFlowWithUserActor_SendsUserActor()
    {
        var actor = ActorTestData.UserActor(ActorTestData.UserId) with { CompanyId = ActorTestData.CompanyId };
        using var actorScope = _holder.Enter(actor);
        using var harness = new ActorHeadersHarness();

        using var response = await harness.CallAsync();

        harness.Resource.ActorHeaders.Should()
            .Equal((ActorTestData.UserId.ToString(), ActorTestData.CompanyId.ToString()));
    }

    [Fact]
    public async Task ActorHeaders_WithIncomingUserToken_ThrowsAndSendsNothing()
    {
        using var actorScope = _holder.Enter(ActorTestData.UserActor(ActorTestData.UserId));
        using var harness = new ActorHeadersHarness();
        harness.Incoming.AccessToken = ServiceTokenTestData.FirstSubjectToken;

        var act = harness.CallAsync;

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(ServiceTokenErrors.ActorHeadersNeedTokenExchange().Message);

        harness.Resource.ActorHeaders.Should().BeEmpty();
    }

    [Fact]
    public async Task ActorHeaders_WithoutActor_ThrowsAndSendsNothing()
    {
        using var harness = new ActorHeadersHarness();

        var act = harness.CallAsync;

        await act.Should()
            .ThrowAsync<InvalidOperationException>()
            .WithMessage(ServiceTokenErrors.ActorHeadersWithoutActor().Message);

        harness.Resource.ActorHeaders.Should().BeEmpty();
    }

    [Fact]
    public async Task ClientCredentialsToken_WithoutActorHeadersHandler_SendsNoActorHeaders()
    {
        using var actorScope = _holder.Enter(ActorTestData.UserActor(ActorTestData.UserId));
        using var harness = new ServiceTokenHarness();

        using var response = await harness.CallAsync();

        harness.Resource.Authorizations.Should().Equal($"Bearer {ServiceTokenTestData.FirstToken}");
        harness.Resource.ActorHeaders.Should().Equal((null, null));
    }
}
