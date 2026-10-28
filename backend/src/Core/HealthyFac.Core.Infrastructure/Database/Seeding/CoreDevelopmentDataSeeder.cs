using Bogus;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Departments;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Companies.Seeding;
using HealthyFac.Core.Infrastructure.Departments.Seeding;
using HealthyFac.Core.Infrastructure.Employees.Seeding;
using HealthyFac.Core.Infrastructure.Machines.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Generates development data from the feature fakers: companies, departments, machines, employees, and open
/// invites. <see cref="DevelopmentDataOptions"/> sets the amounts and the shapes. Each faker gets the next seed
/// from the configured base, so every run produces the same data, and screenshots and bug reports stay
/// comparable. Seeded employees are active with a random identity ID. No Keycloak user exists for them, so nobody
/// can sign in as a seeded employee. The only exception is the optional test tenant (<see cref="TestTenantOptions"/>):
/// its supervisor has the fixed IDs of the Keycloak realm import and can sign in.
/// </summary>
/// <param name="dbContext">The core database context.</param>
/// <param name="dateProvider">The clock for invite expiry and acceptance times.</param>
/// <param name="seedingOptions">The validated seeding options.</param>
/// <param name="logger">The logger for the forced recreation warning.</param>
/// <param name="inviteTokenService">The service that creates the salt of the test tenant invite.</param>
public sealed class CoreDevelopmentDataSeeder(
    CoreDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<SeedingOptions> seedingOptions,
    ILogger<CoreDevelopmentDataSeeder> logger,
    IInviteTokenService inviteTokenService
)
{
    private const int TestTenantDepartmentCount = 2;

    private readonly DevelopmentDataOptions _options = seedingOptions.Value.DevelopmentData;
    private readonly IInviteTokenService _inviteTokenService = inviteTokenService;
    private int _seedOffset;

    /// <summary>
    /// Adds the development data. The generated data enters only when the database holds no company yet, so a
    /// restart does not duplicate it. The test tenant enters when its company does not exist yet. With
    /// <see cref="DevelopmentDataOptions.IsRecreationForced"/>, it first deletes the existing business data and then
    /// adds everything again. Run it after the required roles exist.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_options.IsRecreationForced)
        {
            logger.LogWarning(
                "Forced recreation of development data: deleting all companies, departments, machines, employees, "
                + "and invites.");

            await DeleteBusinessDataAsync(cancellationToken);
        }

        await SeedGeneratedDataAsync(cancellationToken);

        if (_options.TestTenant is not null)
        {
            await SeedTestTenantAsync(_options.TestTenant, cancellationToken);
        }
    }

    private async Task SeedGeneratedDataAsync(CancellationToken cancellationToken)
    {
        var hasCompanies = await dbContext.Companies.AnyAsync(cancellationToken);
        if (hasCompanies)
        {
            return;
        }

        var roleIds = await dbContext.Roles.ToDictionaryAsync(role => role.Code, role => role.Id, cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var inviteLifetime = TimeSpan.FromDays(_options.InviteLifetimeDays);
        var identityRandomizer = new Randomizer(_options.RandomSeed);

        var companies = new CompanyFaker(NextSeed()).Generate(_options.CompanyCount);
        dbContext.Companies.AddRange(companies);
        await dbContext.SaveChangesAsync(cancellationToken);

        var departments = companies
            .SelectMany(company => new DepartmentFaker(
                    company.Id,
                    _options.GetDepartmentNames(),
                    _options.GetTimeZoneIds(),
                    NextSeed())
                .Generate(_options.DepartmentsPerCompany))
            .ToList();
        dbContext.Departments.AddRange(departments);
        await dbContext.SaveChangesAsync(cancellationToken);

        foreach (var department in departments)
        {
            dbContext.Machines.AddRange(
                new MachineFaker(department, _options.GetMachineTypes(), _options.SerialNumberPattern, NextSeed())
                    .Generate(_options.MachinesPerDepartment));

            AddEmployees(
                EmployeeInviteFaker.WithProfile(
                    department,
                    roleIds[RoleCode.Supervisor],
                    now,
                    inviteLifetime,
                    NextSeed()),
                _options.SupervisorsPerDepartment,
                now,
                identityRandomizer);

            AddEmployees(
                EmployeeInviteFaker.WithProfile(department, roleIds[RoleCode.Worker], now, inviteLifetime, NextSeed()),
                _options.WorkersPerDepartment,
                now,
                identityRandomizer);
        }

        foreach (var companyDepartments in departments.GroupBy(department => department.CompanyId))
        {
            dbContext.EmployeeInvites.AddRange(
                EmployeeInviteFaker.Open(
                        companyDepartments.First(),
                        roleIds[RoleCode.Worker],
                        now,
                        inviteLifetime,
                        NextSeed())
                    .Generate(_options.OpenInvitesPerCompany));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTestTenantAsync(TestTenantOptions tenant, CancellationToken cancellationToken)
    {
        var isCompanySeeded = await dbContext.Companies.AnyAsync(
            company => company.Id == tenant.CompanyId,
            cancellationToken);
        if (isCompanySeeded)
        {
            return;
        }

        var supervisorRoleId = await dbContext.Roles
            .Where(role => role.Code == RoleCode.Supervisor)
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var company = new Company { Name = tenant.CompanyName };
        dbContext.Entry(company).Property(entity => entity.Id).CurrentValue = tenant.CompanyId;
        dbContext.Companies.Add(company);

        var departments = _options.GetDepartmentNames()
            .Take(TestTenantDepartmentCount)
            .Select((name, index) => new Department
            {
                CompanyId = tenant.CompanyId,
                Name = name,
                TimeZoneId = _options.GetTimeZoneIds()[index % _options.GetTimeZoneIds().Count],
            })
            .ToList();
        dbContext.Departments.AddRange(departments);

        var invite = new EmployeeInvite(
            tenant.CompanyId,
            tenant.SupervisorEmail,
            supervisorRoleId,
            departments,
            _inviteTokenService.CreateSalt(),
            now.AddDays(_options.InviteLifetimeDays));
        invite.UpdateProfile(tenant.SupervisorFirstName, tenant.SupervisorLastName);

        var supervisor = invite.Accept(now);
        dbContext.Entry(supervisor).Property(entity => entity.Id).CurrentValue = tenant.SupervisorEmployeeId;
        supervisor.Activate(tenant.SupervisorIdentityId);

        dbContext.EmployeeInvites.Add(invite);
        dbContext.Employees.Add(supervisor);

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteBusinessDataAsync(CancellationToken cancellationToken)
    {
        await dbContext.Employees.ExecuteDeleteAsync(cancellationToken);
        await dbContext.EmployeeInvites.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Machines.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Departments.ExecuteDeleteAsync(cancellationToken);
        await dbContext.Companies.ExecuteDeleteAsync(cancellationToken);
    }

    private void AddEmployees(
        EmployeeInviteFaker inviteFaker,
        int count,
        DateTime now,
        Randomizer identityRandomizer
    )
    {
        var invites = inviteFaker.Generate(count);

        foreach (var invite in invites)
        {
            var employee = invite.Accept(now);
            employee.Activate(identityRandomizer.Guid());

            dbContext.EmployeeInvites.Add(invite);
            dbContext.Employees.Add(employee);
        }
    }

    private int NextSeed() => _options.RandomSeed + _seedOffset++;
}
