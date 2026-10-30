using HealthyFac.Core.Application.Employees.Messages;

namespace HealthyFac.Core.UnitTests.Employees.Data;

public static class IdentityProvisioningValidatorTestData
{
    private const string ValidReason = "OrganizationNotFound";

    public static IdentityProvisioned ValidProvisioned => CreateProvisioned();

    public static IdentityProvisioningFailed ValidFailed => CreateFailed();

    public static TheoryData<IdentityProvisioned, string> InvalidProvisionedWithProperty => new()
    {
        { CreateProvisioned(employeeId: Guid.Empty), nameof(IdentityProvisioned.EmployeeId) },
        { CreateProvisioned(identityUserId: Guid.Empty), nameof(IdentityProvisioned.IdentityUserId) },
    };

    public static TheoryData<IdentityProvisioningFailed, string> InvalidFailedWithProperty => new()
    {
        { CreateFailed(employeeId: Guid.Empty), nameof(IdentityProvisioningFailed.EmployeeId) },
        { CreateFailed(reason: string.Empty), nameof(IdentityProvisioningFailed.Reason) },
        { CreateFailed(reason: "   "), nameof(IdentityProvisioningFailed.Reason) },
    };

    private static IdentityProvisioned CreateProvisioned(Guid? employeeId = null, Guid? identityUserId = null) => new()
    {
        EmployeeId = employeeId ?? Guid.CreateVersion7(),
        IdentityUserId = identityUserId ?? Guid.CreateVersion7(),
    };

    private static IdentityProvisioningFailed CreateFailed(
        Guid? employeeId = null,
        string reason = ValidReason
    ) => new()
    {
        EmployeeId = employeeId ?? Guid.CreateVersion7(),
        Reason = reason,
    };
}
