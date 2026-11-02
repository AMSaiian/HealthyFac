using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class KnownActorsTests
{
    [Fact]
    public void KnownActors_EveryActorId_IsVersion5AndUnique()
    {
        var actors = ServiceName.All
            .SelectMany(service => ServiceActor.All.Select(flow => flow.For(service)))
            .Append(KnownActors.Anonymous)
            .ToList();

        var ids = actors.Select(actor => actor.UserId).ToList();

        ids.Should().OnlyHaveUniqueItems();
        ids.Select(id => id.ToString()[KnownActorsTestData.VersionCharacterIndex].ToString())
            .Should()
            .AllBe(KnownActorsTestData.VersionDigits);
    }

    [Theory]
    [MemberData(nameof(KnownActorsTestData.ActorValues), MemberType = typeof(KnownActorsTestData))]
    public void ActorValues_AreCamelCase(string value)
    {
        value.Should().MatchRegex("^[a-z][a-zA-Z0-9]*$");
    }

    [Fact]
    public void IdOf_ForAnonymous_EqualsFixedId()
    {
        var id = KnownActors.IdOf("anonymous");

        id.Should().Be(KnownActorsTestData.AnonymousId);
        KnownActors.Anonymous.UserId.Should().Be(KnownActorsTestData.AnonymousId);
    }

    [Fact]
    public void For_WithServiceName_PrefixesTheName()
    {
        var actor = ServiceActor.OutboxProcessor.For(ServiceName.Core);

        actor.Kind.Should().Be(ActorKind.Service);
        actor.Name.Should().Be(KnownActorsTestData.OutboxProcessorName);
        actor.UserId.Should().Be(KnownActors.IdOf("service:core:outboxProcessor"));
    }

    [Fact]
    public void For_WithCompany_KeepsCompany()
    {
        var actor = ServiceActor.InboxProcessor.For(ServiceName.Auth, ActorTestData.CompanyId);

        actor.CompanyId.Should().Be(ActorTestData.CompanyId);
    }

    [Fact]
    public void Restore_WithServiceActorOfOtherService_ReturnsServiceWithoutName()
    {
        var actorId = ServiceActor.InboxProcessor.For(ServiceName.Auth).UserId;

        var actor = KnownActors.Restore(actorId, ActorTestData.CompanyId);

        actor.Kind.Should().Be(ActorKind.Service);
        actor.Name.Should().BeNull();
        actor.UserId.Should().Be(actorId);
        actor.CompanyId.Should().Be(ActorTestData.CompanyId);
    }

    [Fact]
    public void Restore_WithAnonymousId_ReturnsAnonymous()
    {
        var actor = KnownActors.Restore(KnownActors.Anonymous.UserId, null);

        actor.Kind.Should().Be(ActorKind.Anonymous);
        actor.Name.Should().BeNull();
        actor.CompanyId.Should().BeNull();
    }

    [Fact]
    public void Restore_WithUnlistedVersion5Id_ReturnsService()
    {
        var actor = KnownActors.Restore(KnownActors.IdOf(KnownActorsTestData.UnlistedKey), ActorTestData.CompanyId);

        actor.Kind.Should().Be(ActorKind.Service);
        actor.Name.Should().BeNull();
        actor.CompanyId.Should().Be(ActorTestData.CompanyId);
    }

    [Fact]
    public void Restore_WithUserId_ReturnsUser()
    {
        var actor = KnownActors.Restore(ActorTestData.UserId, ActorTestData.CompanyId);

        actor.Kind.Should().Be(ActorKind.User);
        actor.Name.Should().BeNull();
        actor.UserId.Should().Be(ActorTestData.UserId);
        actor.CompanyId.Should().Be(ActorTestData.CompanyId);
    }

    [Fact]
    public void KindOf_WithAnonymousId_ReturnsAnonymous()
    {
        var kind = KnownActors.KindOf(KnownActors.Anonymous.UserId);

        kind.Should().Be(ActorKind.Anonymous);
    }

    [Fact]
    public void KindOf_WithServiceActorId_ReturnsService()
    {
        var kind = KnownActors.KindOf(ServiceActor.OutboxProcessor.For(ServiceName.Core).UserId);

        kind.Should().Be(ActorKind.Service);
    }

    [Fact]
    public void KindOf_WithUserId_ReturnsUser()
    {
        var kind = KnownActors.KindOf(KnownActorsTestData.KeycloakUserId);

        kind.Should().Be(ActorKind.User);
    }
}
