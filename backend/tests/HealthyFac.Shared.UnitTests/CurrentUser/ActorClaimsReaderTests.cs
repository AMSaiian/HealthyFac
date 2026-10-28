using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;
using HealthyFac.Shared.Web.CurrentUser;
using UnauthorizedAccessException = HealthyFac.Shared.Application.ErrorHandling.UnauthorizedAccessException;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorClaimsReaderTests
{
    [Fact]
    public void Read_WithValidClaims_ReturnsActor()
    {
        var actor = ActorClaimsReader.Read(ActorClaimsTestData.ValidPrincipal);

        actor.Should()
            .Be(
                new ActorContext
                {
                    UserId = ActorTestData.UserId,
                    CompanyId = ActorTestData.CompanyId,
                    EmployeeId = ActorTestData.EmployeeId,
                });
    }

    [Fact]
    public void Read_WithTwoOrganizations_Throws()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            [ActorTestData.CompanyId.ToString(), ActorTestData.OtherCompanyId.ToString()],
            ActorTestData.EmployeeId.ToString());

        var act = () => ActorClaimsReader.Read(principal);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.OrganizationInvalid().Message);
    }

    [Theory]
    [MemberData(
        nameof(ActorClaimsTestData.JsonOrganizationValues),
        MemberType = typeof(ActorClaimsTestData)
    )]
    public void Read_WithJsonOrganizationValue_Throws(string organization)
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            [organization],
            ActorTestData.EmployeeId.ToString());

        var act = () => ActorClaimsReader.Read(principal);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.OrganizationInvalid().Message);
    }

    [Fact]
    public void Read_WithNoOrganization_Throws()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            ActorTestData.UserId.ToString(),
            [],
            ActorTestData.EmployeeId.ToString());

        var act = () => ActorClaimsReader.Read(principal);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.OrganizationInvalid().Message);
    }

    [Fact]
    public void Read_WithNonGuidSubject_Throws()
    {
        var principal = ActorClaimsTestData.CreatePrincipal(
            "not-a-guid",
            [ActorTestData.CompanyId.ToString()],
            ActorTestData.EmployeeId.ToString());

        var act = () => ActorClaimsReader.Read(principal);

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

        var act = () => ActorClaimsReader.Read(principal);

        act.Should()
            .Throw<UnauthorizedAccessException>()
            .WithMessage(CurrentUserErrors.EmployeeIdInvalid().Message);
    }
}
