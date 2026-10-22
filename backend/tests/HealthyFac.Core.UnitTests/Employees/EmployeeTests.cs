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
    public void RetryProvisioning_AfterFailure_ReturnsToProvisioning()
    {
        var employee = EmployeeTestData.CreateEmployee();
        employee.MarkProvisioningFailed();

        employee.RetryProvisioning();

        employee.Status.Should().Be(EmployeeStatus.Provisioning);
    }

    [Fact]
    public void RetryProvisioning_WhenProvisioning_ThrowsInvalidOperationException()
    {
        var employee = EmployeeTestData.CreateEmployee();

        var act = employee.RetryProvisioning;

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

    [Fact]
    public void AssignDepartment_WithDepartmentOfSameCompany_AddsDepartment()
    {
        var employee = EmployeeTestData.CreateEmployee();
        var department = EmployeeTestData.CreateDepartment();

        employee.AssignDepartment(department);

        employee.Departments.Should().HaveCount(2).And.Contain(department);
    }

    [Fact]
    public void AssignDepartment_WithAssignedDepartment_KeepsOneLink()
    {
        var employee = EmployeeTestData.CreateEmployee();
        var department = EmployeeTestData.CreateDepartment();
        employee.AssignDepartment(department);

        employee.AssignDepartment(department);

        employee.Departments.Should().HaveCount(2);
    }

    [Fact]
    public void AssignDepartment_WithDepartmentOfOtherCompany_ThrowsAndKeepsDepartments()
    {
        var employee = EmployeeTestData.CreateEmployee();
        var department = EmployeeTestData.CreateDepartmentOfOtherCompany();

        var act = () => employee.AssignDepartment(department);

        act.Should().Throw<InvalidOperationException>();
        employee.Departments.Should().ContainSingle();
    }

    [Fact]
    public void UnassignDepartment_WithOneOfTwoDepartments_RemovesIt()
    {
        var employee = EmployeeTestData.CreateEmployee();
        var department = EmployeeTestData.CreateDepartment();
        employee.AssignDepartment(department);

        employee.UnassignDepartment(department);

        employee.Departments.Should().ContainSingle().Which.Should().NotBeSameAs(department);
    }

    [Fact]
    public void UnassignDepartment_WithLastDepartment_ThrowsAndKeepsIt()
    {
        var employee = EmployeeTestData.CreateEmployee();
        var lastDepartment = employee.Departments.Single();

        var act = () => employee.UnassignDepartment(lastDepartment);

        act.Should().Throw<InvalidOperationException>();
        employee.Departments.Should().ContainSingle().Which.Should().BeSameAs(lastDepartment);
    }

    [Fact]
    public void UnassignDepartment_WithUnassignedDepartment_ChangesNothing()
    {
        var employee = EmployeeTestData.CreateEmployee();
        var assignedDepartment = employee.Departments.Single();

        employee.UnassignDepartment(EmployeeTestData.CreateDepartment());

        employee.Departments.Should().ContainSingle().Which.Should().BeSameAs(assignedDepartment);
    }
}
