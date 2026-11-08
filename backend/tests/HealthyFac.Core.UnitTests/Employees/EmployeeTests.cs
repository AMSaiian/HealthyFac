using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.UnitTests.Employees.Data;

namespace HealthyFac.Core.UnitTests.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Activate_WhenProvisioning_SetsActiveAndLinksIdentity()
    {
        var employee = EmployeeTestData.CreateEmployee();

        employee.Activate(EmployeeTestData.IdentityUserId);

        employee.Status.Should().Be(EmployeeStatus.Active);
        employee.IdentityUserId.Should().Be(EmployeeTestData.IdentityUserId);
    }

    [Theory]
    [MemberData(
        nameof(EmployeeTestData.TransitionsOutOfProvisioning),
        MemberType = typeof(EmployeeTestData)
    )]
    public void Activate_WhenNotProvisioning_ThrowsInvalidOperationException(Action<Employee> transition)
    {
        var employee = EmployeeTestData.CreateEmployee();
        transition(employee);

        var act = () => employee.Activate(EmployeeTestData.IdentityUserId);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void LinkIdentity_WithSameIdentityTwice_KeepsIdentity()
    {
        var employee = EmployeeTestData.CreateEmployee();
        employee.LinkIdentity(EmployeeTestData.IdentityUserId);

        employee.LinkIdentity(EmployeeTestData.IdentityUserId);

        employee.IdentityUserId.Should().Be(EmployeeTestData.IdentityUserId);
    }

    [Fact]
    public void LinkIdentity_WithOtherIdentity_ThrowsAndKeepsFirstIdentity()
    {
        var employee = EmployeeTestData.CreateEmployee();
        employee.LinkIdentity(EmployeeTestData.IdentityUserId);

        var act = () => employee.LinkIdentity(EmployeeTestData.OtherIdentityUserId);

        act.Should().Throw<InvalidOperationException>();
        employee.IdentityUserId.Should().Be(EmployeeTestData.IdentityUserId);
    }

    [Fact]
    public void Activate_WithIdentityLinkedToOtherUser_ThrowsAndStaysProvisioning()
    {
        var employee = EmployeeTestData.CreateEmployee();
        employee.LinkIdentity(EmployeeTestData.IdentityUserId);

        var act = () => employee.Activate(EmployeeTestData.OtherIdentityUserId);

        act.Should().Throw<InvalidOperationException>();
        employee.Status.Should().Be(EmployeeStatus.Provisioning);
    }

    [Fact]
    public void RetryProvisioning_ForProvisioningEmployee_StaysProvisioningAndSetsRetriedAt()
    {
        var employee = EmployeeTestData.CreateEmployee();

        employee.RetryProvisioning(EmployeeTestData.RetriedAt);

        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        employee.ProvisioningRetriedAt.Should().Be(EmployeeTestData.RetriedAt);
    }

    [Fact]
    public void RetryProvisioning_ForFailedEmployee_SetsProvisioningAndRetriedAt()
    {
        var employee = EmployeeTestData.CreateEmployee();
        employee.MarkProvisioningFailed();

        employee.RetryProvisioning(EmployeeTestData.RetriedAt);

        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        employee.ProvisioningRetriedAt.Should().Be(EmployeeTestData.RetriedAt);
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.RetryAbilityByStatus), MemberType = typeof(EmployeeTestData))]
    public void CanRetryProvisioning_ForEachStatus_ReturnsExpected(Action<Employee> transition, bool canRetry)
    {
        var employee = EmployeeTestData.CreateEmployee();
        transition(employee);

        var result = employee.CanRetryProvisioning;

        result.Should().Be(canRetry);
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.DeactivationAbilityByStatus), MemberType = typeof(EmployeeTestData))]
    public void CanDeactivate_ForEachStatus_ReturnsExpected(Action<Employee> transition, bool canDeactivate)
    {
        var employee = EmployeeTestData.CreateEmployee();
        transition(employee);

        var result = employee.CanDeactivate;

        result.Should().Be(canDeactivate);
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.IdentityLinkCases), MemberType = typeof(EmployeeTestData))]
    public void IsLinkedTo_ForEachCase_ReturnsExpected(Guid? linkedIdentity, Guid? identityUserId, bool isLinked)
    {
        var employee = EmployeeTestData.CreateEmployee();
        if (linkedIdentity is { } identity)
        {
            employee.LinkIdentity(identity);
        }

        var result = employee.IsLinkedTo(identityUserId);

        result.Should().Be(isLinked);
    }

    [Theory]
    [MemberData(
        nameof(EmployeeTestData.TransitionsToStatusThatCannotRetry),
        MemberType = typeof(EmployeeTestData)
    )]
    public void RetryProvisioning_WhenActiveOrDeactivated_ThrowsInvalidOperationException(Action<Employee> transition)
    {
        var employee = EmployeeTestData.CreateEmployee();
        transition(employee);

        var act = () => employee.RetryProvisioning(EmployeeTestData.RetriedAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Deactivate_WhenAlreadyDeactivated_ThrowsInvalidOperationException()
    {
        var employee = EmployeeTestData.CreateEmployee();
        employee.Deactivate();

        var act = employee.Deactivate;

        act.Should().Throw<InvalidOperationException>();
    }
}
