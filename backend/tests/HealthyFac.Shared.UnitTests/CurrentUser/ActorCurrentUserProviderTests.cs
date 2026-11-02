using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Infrastructure.CurrentUser;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorCurrentUserProviderTests
{
    [Fact]
    public void GetUserId_WithoutActor_ThrowsUnauthorized()
    {
        var provider = new ActorCurrentUserProvider(new ActorContextHolder());

        var act = provider.GetUserId;

        act.Should().Throw<UnauthorizedAccessException>();
    }

    [Fact]
    public void GetUserId_WithActor_ReturnsActorUserId()
    {
        var holder = new ActorContextHolder();
        using var actorScope = holder.Enter(ActorTestData.UserActor(ActorTestData.UserId));
        var provider = new ActorCurrentUserProvider(holder);

        var userId = provider.GetUserId();

        userId.Should().Be(ActorTestData.UserId);
    }

    [Fact]
    public void Enter_WhileActorIsSet_Throws()
    {
        var holder = new ActorContextHolder();
        using var actorScope = holder.Enter(ActorTestData.UserActor(ActorTestData.UserId));

        var act = () => holder.Enter(ActorTestData.UserActor(ActorTestData.OtherUserId));

        act.Should().Throw<InvalidOperationException>();
        holder.Current!.UserId.Should().Be(ActorTestData.UserId);
    }

    [Fact]
    public void Enter_AfterDispose_SetsNewActor()
    {
        var holder = new ActorContextHolder();
        var firstActorScope = holder.Enter(ActorTestData.UserActor(ActorTestData.UserId));
        firstActorScope.Dispose();

        using var secondActorScope = holder.Enter(ActorTestData.UserActor(ActorTestData.OtherUserId));

        holder.Current!.UserId.Should().Be(ActorTestData.OtherUserId);
    }

    [Fact]
    public void GetCompanyId_WithServiceActorWithoutCompany_ThrowsUnauthorized()
    {
        var holder = new ActorContextHolder();
        using var actorScope = holder.Enter(ServiceActor.OutboxProcessor.For(ServiceName.Core));
        var provider = new ActorCurrentUserProvider(holder);

        var act = provider.GetCompanyId;

        act.Should().Throw<UnauthorizedAccessException>();
    }
}
