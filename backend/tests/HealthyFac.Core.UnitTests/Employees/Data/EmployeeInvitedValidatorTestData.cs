using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.Messages;

namespace HealthyFac.Core.UnitTests.Employees.Data;

public static class EmployeeInvitedValidatorTestData
{
    private const string ValidEmail = "worker@plant.com";
    private const string EmailDomain = "@plant.com";

    public static EmployeeInvited ValidMessage => Create();

    public static TheoryData<EmployeeInvited, string> InvalidMessagesWithProperty => new()
    {
        { Create(inviteId: Guid.Empty), nameof(EmployeeInvited.InviteId) },
        { Create(companyId: Guid.Empty), nameof(EmployeeInvited.CompanyId) },
        { Create(email: string.Empty), nameof(EmployeeInvited.Email) },
        { Create(email: "   "), nameof(EmployeeInvited.Email) },
        { Create(email: "not-an-address"), nameof(EmployeeInvited.Email) },
        {
            Create(email: CreateEmailOfLength(EmployeeValidationConstants.EmailMaxLength + 1)),
            nameof(EmployeeInvited.Email)
        },
    };

    private static string CreateEmailOfLength(int length) =>
        new string('a', length - EmailDomain.Length) + EmailDomain;

    private static EmployeeInvited Create(
        Guid? inviteId = null,
        Guid? companyId = null,
        string email = ValidEmail
    ) => new()
    {
        InviteId = inviteId ?? Guid.CreateVersion7(),
        CompanyId = companyId ?? Guid.CreateVersion7(),
        Email = email,
    };
}
