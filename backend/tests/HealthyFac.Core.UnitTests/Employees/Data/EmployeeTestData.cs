using Bogus;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Infrastructure.Database.Seeding;
using HealthyFac.Core.Infrastructure.Departments.Seeding;

namespace HealthyFac.Core.UnitTests.Employees.Data;

public static class EmployeeTestData
{
    public const string Email = "jane.doe@example.com";

    private const int RandomSeed = 20261021;

    private static readonly Faker Faker = new() { Random = new Randomizer(RandomSeed) };

    public static readonly string FirstName = Faker.Name.FirstName();
    public static readonly string LastName = Faker.Name.LastName();
    public static readonly Guid CompanyId = Faker.Random.Guid();
    public static readonly Guid OtherCompanyId = Faker.Random.Guid();
    public static readonly Guid RoleId = Faker.Random.Guid();
    public static readonly Guid IdentityUserId = Faker.Random.Guid();
    public static readonly Guid OtherIdentityUserId = Faker.Random.Guid();
    public static readonly byte[] TokenSalt = Faker.Random.Bytes(32);
    public static readonly byte[] RenewedTokenSalt = Faker.Random.Bytes(32);
    public static readonly DateTime BeforeExpiry = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);
    public static readonly DateTime ExpiresAt = BeforeExpiry.AddDays(7);
    public static readonly DateTime RenewedExpiresAt = BeforeExpiry.AddDays(14);

    private static readonly DevelopmentDataOptions DefaultShapes = new();

    private static readonly DepartmentFaker CompanyDepartments = new(
        CompanyId,
        DefaultShapes.GetDepartmentNames(),
        DefaultShapes.GetTimeZoneIds(),
        RandomSeed);

    private static readonly DepartmentFaker OtherCompanyDepartments = new(
        OtherCompanyId,
        DefaultShapes.GetDepartmentNames(),
        DefaultShapes.GetTimeZoneIds(),
        RandomSeed);

    public static TheoryData<string> EmailsToNormalize =>
    [
        "Jane.Doe@Example.com",
        "  jane.doe@example.com  ",
        "JANE.DOE@EXAMPLE.COM",
    ];

    public static TheoryData<string?, string?> IncompleteProfiles => new()
    {
        { null, LastName },
        { FirstName, null },
        { "   ", LastName },
        { FirstName, "" },
    };

    public static TheoryData<DateTime> TimesAtOrAfterExpiry =>
    [
        ExpiresAt,
        ExpiresAt.AddSeconds(1),
    ];

    public static TheoryData<Action<Employee>> TransitionsOutOfProvisioning =>
    [
        employee => employee.Activate(IdentityUserId),
        employee => employee.MarkProvisioningFailed(),
        employee => employee.Deactivate(),
    ];

    public static EmployeeInvite CreateInvite(string email = Email)
        => new(CompanyId, email, RoleId, [CompanyDepartments.Generate()], TokenSalt, ExpiresAt);

    public static Department CreateDepartmentOfOtherCompany() => OtherCompanyDepartments.Generate();

    public static Department CreateDepartment() => CompanyDepartments.Generate();

    public static EmployeeInvite CreateInviteWithProfile()
    {
        var invite = CreateInvite();
        invite.UpdateProfile(FirstName, LastName);

        return invite;
    }

    public static Employee CreateEmployee() => CreateInviteWithProfile().Accept(BeforeExpiry);
}
