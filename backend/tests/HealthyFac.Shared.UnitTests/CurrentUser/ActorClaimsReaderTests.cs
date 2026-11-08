using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Shared.Web.CurrentUser;
using HealthyFac.Shared.Web.CurrentUser.ServiceActors;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorClaimsReaderTests
{
    [Fact]
    public void Read_WithValidClaims_ReturnsActor()
    {
        var actor = ActorClaimsReader.Read(
            ActorClaimsTestData.ValidPrincipal,
            ActorClaimsTestData.NoHeaders,
            ActorClaimsTestData.NoServiceClients);

        actor.Should()
            .Be(
                new ActorContext
                {
                    Kind = ActorKind.User,
                    UserId = ActorTestData.UserId,
                    CompanyId = ActorTestData.CompanyId,
                    EmployeeId = ActorTestData.EmployeeId,
                });
    }

    [Theory]
    [MemberData(nameof(ActorClaimsTestData.InvalidOrganizations), MemberType = typeof(ActorClaimsTestData))]
    public void Read_WithInvalidOrganizationClaims_Throws(string[] organizations)
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            organizations,
            ActorTestData.EmployeeId.ToString());

        var act = () => ActorClaimsReader.Read(
            principal,
            ActorClaimsTestData.NoHeaders,
            ActorClaimsTestData.NoServiceClients);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.OrganizationInvalid().Message);
    }

    [Fact]
    public void Read_WithNonGuidSubject_Throws()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            subject: "not-a-guid",
            [ActorTestData.CompanyId.ToString()],
            ActorTestData.EmployeeId.ToString());

        var act = () => ActorClaimsReader.Read(
            principal,
            ActorClaimsTestData.NoHeaders,
            ActorClaimsTestData.NoServiceClients);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.SubjectInvalid().Message);
    }

    [Fact]
    public void Read_WithMissingEmployeeId_Throws()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            [ActorTestData.CompanyId.ToString()],
            employeeId: null);

        var act = () => ActorClaimsReader.Read(
            principal,
            ActorClaimsTestData.NoHeaders,
            ActorClaimsTestData.NoServiceClients);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.EmployeeIdInvalid().Message);
    }

    [Fact]
    public void Read_WithAllowedServiceClientAndActorHeader_ReturnsRestoredActor()
    {
        var headers = ActorClaimsTestData.ActorHeaders(
            ActorClaimsTestData.CallerActor.UserId.ToString(),
            ActorTestData.CompanyId.ToString());

        var actor = ActorClaimsReader.Read(
            IncomingTokenTestData.ServicePrincipal,
            headers,
            ActorClaimsTestData.AllowedServiceClients);

        actor.Should().Be(ActorClaimsTestData.CallerActor with { Name = null, CompanyId = ActorTestData.CompanyId });
    }

    [Fact]
    public void Read_WithServiceTokenAndUserActorHeader_RestoresUser()
    {
        var headers = ActorClaimsTestData.ActorHeaders(
            ActorTestData.UserId.ToString(),
            ActorTestData.CompanyId.ToString());

        var actor = ActorClaimsReader.Read(
            IncomingTokenTestData.ServicePrincipal,
            headers,
            ActorClaimsTestData.AllowedServiceClients);

        actor.Should()
            .Be(
                new ActorContext
                {
                    Kind = ActorKind.User,
                    UserId = ActorTestData.UserId,
                    CompanyId = ActorTestData.CompanyId,
                });
    }

    [Fact]
    public void Read_WithServiceTokenWithoutActorHeader_Throws()
    {
        var act = () => ActorClaimsReader.Read(
            IncomingTokenTestData.ServicePrincipal,
            ActorClaimsTestData.NoHeaders,
            ActorClaimsTestData.AllowedServiceClients);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.ServiceActorMissing().Message);
    }

    [Theory]
    [MemberData(nameof(ActorClaimsTestData.InvalidActorHeaders), MemberType = typeof(ActorClaimsTestData))]
    public void Read_WithServiceTokenAndInvalidActorHeader_Throws(string actorId)
    {
        var act = () => ActorClaimsReader.Read(
            IncomingTokenTestData.ServicePrincipal,
            ActorClaimsTestData.ActorHeaders(actorId),
            ActorClaimsTestData.AllowedServiceClients);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.ServiceActorMissing().Message);
    }

    [Fact]
    public void Read_WithServiceTokenAndInvalidCompanyHeader_Throws()
    {
        var headers = ActorClaimsTestData.ActorHeaders(ActorTestData.UserId.ToString(), companyId: "not-a-guid");

        var act = () => ActorClaimsReader.Read(
            IncomingTokenTestData.ServicePrincipal,
            headers,
            ActorClaimsTestData.AllowedServiceClients);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.ServiceActorMissing().Message);
    }

    [Theory]
    [MemberData(nameof(ActorClaimsTestData.OptionsWithoutServiceClient), MemberType = typeof(ActorClaimsTestData))]
    public void Read_WithServiceClientNotAllowed_Throws(ServiceActorOptions options)
    {
        var headers = ActorClaimsTestData.ActorHeaders(ActorTestData.UserId.ToString());

        var act = () => ActorClaimsReader.Read(IncomingTokenTestData.ServicePrincipal, headers, options);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.ServiceClientNotAllowed(IncomingTokenTestData.ServiceClientId).Message);
    }

    [Fact]
    public void Read_WithClientIdAndUserClaims_ReadsUserAndIgnoresHeaders()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            [ActorTestData.CompanyId.ToString()],
            ActorTestData.EmployeeId.ToString(),
            IncomingTokenTestData.ServiceClientId);
        var headers = ActorClaimsTestData.ActorHeaders(
            ActorTestData.OtherUserId.ToString(),
            ActorTestData.OtherCompanyId.ToString());

        var actor = ActorClaimsReader.Read(principal, headers, ActorClaimsTestData.NoServiceClients);

        actor.Should()
            .Be(
                new ActorContext
                {
                    Kind = ActorKind.User,
                    UserId = ActorTestData.UserId,
                    CompanyId = ActorTestData.CompanyId,
                    EmployeeId = ActorTestData.EmployeeId,
                });
    }

    [Fact]
    public void Read_WithUnknownClaimOfInvalidValue_ReturnsValidActor()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            [ActorTestData.CompanyId.ToString()],
            ActorTestData.EmployeeId.ToString(),
            legacyClaimValue: ActorClaimsTestData.LegacyClaimValue);

        var actor = ActorClaimsReader.Read(
            principal,
            ActorClaimsTestData.NoHeaders,
            ActorClaimsTestData.NoServiceClients);

        actor.CompanyId.Should().Be(ActorTestData.CompanyId);
        actor.EmployeeId.Should().Be(ActorTestData.EmployeeId);
    }
}
