using Bogus;
using HealthyFac.Core.Domain.Employees;

namespace HealthyFac.Core.UnitTests.Employees.Data;

public static class EmployeeTestData
{
    public const string Email = "jane.doe@example.com";

    private const int RandomSeed = 20261021;

    private static readonly Faker Faker = new() { Random = new Randomizer(RandomSeed) };

    public static readonly string FirstName = Faker.Name.FirstName();
    public static readonly string LastName = Faker.Name.LastName();
    public static readonly Guid CompanyId = Faker.Random.Guid();
    public static readonly Guid RoleId = Faker.Random.Guid();
    public static readonly Guid IdentityUserId = Faker.Random.Guid();
    public static readonly Guid OtherIdentityUserId = Faker.Random.Guid();
    public static readonly byte[] TokenSalt = Faker.Random.Bytes(count: 32);
    public static readonly byte[] RenewedTokenSalt = Faker.Random.Bytes(count: 32);

    public static readonly DateTime BeforeExpiry = new(
        year: 2026, month: 10, day: 21, hour: 12, minute: 0, second: 0, DateTimeKind.Utc);

    public static readonly DateTime ExpiresAt = BeforeExpiry.AddDays(value: 7);
    public static readonly DateTime RenewedExpiresAt = BeforeExpiry.AddDays(value: 14);
    public static readonly DateTime RetriedAt = BeforeExpiry.AddDays(value: 1);

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
        ExpiresAt.AddSeconds(value: 1),
    ];

    public static TheoryData<Action<Employee>> TransitionsOutOfProvisioning => new()
    {
        employee => employee.Activate(IdentityUserId),
        employee => employee.MarkProvisioningFailed(),
        employee => employee.Deactivate(),
    };

    public static TheoryData<Action<Employee>, bool> RetryAbilityByStatus => new()
    {
        { _ => { }, true },
        { employee => employee.MarkProvisioningFailed(), true },
        { employee => employee.Activate(IdentityUserId), false },
        { employee => employee.Deactivate(), false },
    };

    public static TheoryData<Action<Employee>, bool> DeactivationAbilityByStatus => new()
    {
        { _ => { }, true },
        { employee => employee.MarkProvisioningFailed(), true },
        { employee => employee.Activate(IdentityUserId), true },
        { employee => employee.Deactivate(), false },
    };

    public static TheoryData<Guid?, Guid?, bool> IdentityLinkCases => new()
    {
        { IdentityUserId, IdentityUserId, true },
        { IdentityUserId, OtherIdentityUserId, false },
        { IdentityUserId, null, false },
        { null, IdentityUserId, false },
        { null, null, false },
    };

    public static TheoryData<Action<Employee>> TransitionsToStatusThatCannotRetry => new()
    {
        employee => employee.Activate(IdentityUserId),
        employee => employee.Deactivate(),
    };

    public static EmployeeInvite CreateInvite(string email = Email)
        => new(CompanyId, email, RoleId, TokenSalt, ExpiresAt);

    public static EmployeeInvite CreateInviteWithProfile()
    {
        var invite = CreateInvite();
        invite.UpdateProfile(FirstName, LastName);

        return invite;
    }

    public static Employee CreateEmployee() => CreateInviteWithProfile().Accept(BeforeExpiry);
}
