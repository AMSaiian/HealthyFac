using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.UnitTests.CurrentUser.Data;

namespace HealthyFac.Shared.UnitTests.CurrentUser;

public sealed class ActorContextScopeChecksTests
{
    [Fact]
    public void EnsureDepartments_WithForeignDepartment_ThrowsForbidden()
    {
        var actor = ActorTestData.DepartmentActor(ActorTestData.FirstDepartmentId);

        var act = () => actor.EnsureDepartments([ActorTestData.FirstDepartmentId, ActorTestData.ForeignDepartmentId]);

        act.Should()
            .Throw<ForbiddenAccessException>()
            .WithMessage(CurrentUserErrors.DepartmentsOutOfScope().Message);
    }

    [Fact]
    public void EnsureDepartments_WithOwnDepartments_Passes()
    {
        var actor = ActorTestData.DepartmentActor(
            ActorTestData.FirstDepartmentId,
            ActorTestData.SecondDepartmentId);

        var act = () => actor.EnsureDepartments([ActorTestData.SecondDepartmentId, ActorTestData.FirstDepartmentId]);

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureCompany_WithOtherCompany_ThrowsForbidden()
    {
        var actor = ActorTestData.DepartmentActor();

        var act = () => actor.EnsureCompany(ActorTestData.OtherCompanyId);

        act.Should()
            .Throw<ForbiddenAccessException>()
            .WithMessage(CurrentUserErrors.CompanyOutOfScope().Message);
    }

    [Fact]
    public void EnsureCompany_WithOwnCompany_Passes()
    {
        var actor = ActorTestData.DepartmentActor();

        var act = () => actor.EnsureCompany(ActorTestData.CompanyId);

        act.Should().NotThrow();
    }
}
