using Bogus;
using HealthyFac.Core.Domain.Employees;

namespace HealthyFac.Core.Infrastructure.Employees.Seeding;

/// <summary>
/// Generates development invites for one company and one role. The email comes from the generated person and
/// the global Bogus index, so it stays unique across all companies, as the unique email index requires. Employees
/// have no own faker, because only <see cref="EmployeeInvite.Accept"/> creates one: accept an invite from
/// <see cref="WithProfile"/> to get an employee. The salt length is fixed and not configurable, because the invite
/// token signer depends on it.
/// </summary>
internal sealed class EmployeeInviteFaker : Faker<EmployeeInvite>
{
    private const int TokenSaltLength = 32;

    private EmployeeInviteFaker(
        Guid companyId,
        Guid roleId,
        DateTime now,
        TimeSpan lifetime,
        bool hasProfile,
        int seed
    )
    {
        UseSeed(seed);
        CustomInstantiator(faker => new EmployeeInvite(
            companyId,
            faker.Internet.Email(
                faker.Person.FirstName,
                faker.Person.LastName,
                uniqueSuffix: faker.IndexGlobal.ToString()),
            roleId,
            faker.Random.Bytes(TokenSaltLength),
            now.Add(lifetime)));

        if (hasProfile)
        {
            FinishWith((faker, invite) => invite.UpdateProfile(faker.Person.FirstName, faker.Person.LastName));
        }
    }

    /// <summary>
    /// Creates a faker for invites with a complete profile, so each invite can be accepted at once.
    /// </summary>
    /// <param name="companyId">The saved company that the invited person joins.</param>
    /// <param name="roleId">The role that the invited person gets.</param>
    /// <param name="now">The current UTC time, the base of the expiry.</param>
    /// <param name="lifetime">The time until the invite expires.</param>
    /// <param name="seed">The random seed, so every run produces the same invites.</param>
    /// <returns>The faker for acceptable invites.</returns>
    public static EmployeeInviteFaker WithProfile(
        Guid companyId,
        Guid roleId,
        DateTime now,
        TimeSpan lifetime,
        int seed
    )
        => new(companyId, roleId, now, lifetime, hasProfile: true, seed);

    /// <summary>
    /// Creates a faker for open invites without a profile, which wait for the invited person.
    /// </summary>
    /// <param name="companyId">The saved company that the invited person joins.</param>
    /// <param name="roleId">The role that the invited person gets.</param>
    /// <param name="now">The current UTC time, the base of the expiry.</param>
    /// <param name="lifetime">The time until the invite expires.</param>
    /// <param name="seed">The random seed, so every run produces the same invites.</param>
    /// <returns>The faker for open invites.</returns>
    public static EmployeeInviteFaker Open(
        Guid companyId,
        Guid roleId,
        DateTime now,
        TimeSpan lifetime,
        int seed
    )
        => new(companyId, roleId, now, lifetime, hasProfile: false, seed);
}
