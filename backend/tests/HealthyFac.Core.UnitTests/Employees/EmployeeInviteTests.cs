using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.UnitTests.Employees.Data;

namespace HealthyFac.Core.UnitTests.Employees;

public sealed class EmployeeInviteTests
{
    [Fact]
    public void Constructor_WithoutDepartments_ThrowsInvalidOperationException()
    {
        var act = () => new EmployeeInvite(
            EmployeeTestData.CompanyId,
            EmployeeTestData.Email,
            EmployeeTestData.RoleId,
            [],
            EmployeeTestData.TokenSalt,
            EmployeeTestData.ExpiresAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Constructor_WithDepartmentOfOtherCompany_ThrowsInvalidOperationException()
    {
        var act = () => new EmployeeInvite(
            EmployeeTestData.CompanyId,
            EmployeeTestData.Email,
            EmployeeTestData.RoleId,
            [EmployeeTestData.CreateDepartment(), EmployeeTestData.CreateDepartmentOfOtherCompany()],
            EmployeeTestData.TokenSalt,
            EmployeeTestData.ExpiresAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.EmailsToNormalize), MemberType = typeof(EmployeeTestData))]
    public void Constructor_WithUntrimmedOrMixedCaseEmail_StoresTrimmedLowerCaseEmail(string email)
    {
        var invite = EmployeeTestData.CreateInvite(email);

        invite.Email.Should().Be(EmployeeTestData.Email);
    }

    [Fact]
    public void Accept_WithCompleteProfileBeforeExpiry_ReturnsProvisioningEmployeeWithInviteData()
    {
        var invite = EmployeeTestData.CreateInviteWithProfile();

        var employee = invite.Accept(EmployeeTestData.BeforeExpiry);

        invite.AcceptedAt.Should().Be(EmployeeTestData.BeforeExpiry);
        employee.CompanyId.Should().Be(EmployeeTestData.CompanyId);
        employee.Email.Should().Be(EmployeeTestData.Email);
        employee.FirstName.Should().Be(EmployeeTestData.FirstName);
        employee.LastName.Should().Be(EmployeeTestData.LastName);
        employee.RoleId.Should().Be(EmployeeTestData.RoleId);
        employee.Departments.Should().BeEquivalentTo(invite.Departments);
        employee.Status.Should().Be(EmployeeStatus.Provisioning);
        employee.IdentityUserId.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.IncompleteProfiles), MemberType = typeof(EmployeeTestData))]
    public void Accept_WithIncompleteProfile_ThrowsAndStaysOpen(string? firstName, string? lastName)
    {
        var invite = EmployeeTestData.CreateInvite();
        invite.UpdateProfile(firstName, lastName);

        var act = () => invite.Accept(EmployeeTestData.BeforeExpiry);

        act.Should().Throw<InvalidOperationException>();
        invite.AcceptedAt.Should().BeNull();
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.TimesAtOrAfterExpiry), MemberType = typeof(EmployeeTestData))]
    public void Accept_AtOrAfterExpiry_ThrowsAndStaysOpen(DateTime acceptedAt)
    {
        var invite = EmployeeTestData.CreateInviteWithProfile();

        var act = () => invite.Accept(acceptedAt);

        act.Should().Throw<InvalidOperationException>();
        invite.AcceptedAt.Should().BeNull();
    }

    [Fact]
    public void Accept_WhenAlreadyAccepted_ThrowsInvalidOperationException()
    {
        var invite = EmployeeTestData.CreateInviteWithProfile();
        invite.Accept(EmployeeTestData.BeforeExpiry);

        var act = () => invite.Accept(EmployeeTestData.BeforeExpiry);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void CanAccept_WhenOpenBeforeExpiry_ReturnsTrue()
    {
        EmployeeTestData.CreateInvite().CanAccept(EmployeeTestData.BeforeExpiry).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(EmployeeTestData.TimesAtOrAfterExpiry), MemberType = typeof(EmployeeTestData))]
    public void CanAccept_AtOrAfterExpiry_ReturnsFalse(DateTime now)
    {
        EmployeeTestData.CreateInvite().CanAccept(now).Should().BeFalse();
    }

    [Fact]
    public void CanAccept_WhenAccepted_ReturnsFalse()
    {
        var invite = EmployeeTestData.CreateInviteWithProfile();
        invite.Accept(EmployeeTestData.BeforeExpiry);

        invite.CanAccept(EmployeeTestData.BeforeExpiry).Should().BeFalse();
    }

    [Fact]
    public void Renew_WhenOpen_ReplacesSaltAndExpiry()
    {
        var invite = EmployeeTestData.CreateInvite();

        invite.Renew(EmployeeTestData.RenewedTokenSalt, EmployeeTestData.RenewedExpiresAt);

        invite.TokenSalt.Should().Equal(EmployeeTestData.RenewedTokenSalt);
        invite.ExpiresAt.Should().Be(EmployeeTestData.RenewedExpiresAt);
    }

    [Fact]
    public void Renew_WhenAccepted_ThrowsInvalidOperationException()
    {
        var invite = EmployeeTestData.CreateInviteWithProfile();
        invite.Accept(EmployeeTestData.BeforeExpiry);

        var act = () => invite.Renew(EmployeeTestData.RenewedTokenSalt, EmployeeTestData.RenewedExpiresAt);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void UpdateProfile_WhenAccepted_ThrowsInvalidOperationException()
    {
        var invite = EmployeeTestData.CreateInviteWithProfile();
        invite.Accept(EmployeeTestData.BeforeExpiry);

        var act = () => invite.UpdateProfile(EmployeeTestData.FirstName, EmployeeTestData.LastName);

        act.Should().Throw<InvalidOperationException>();
    }
}
