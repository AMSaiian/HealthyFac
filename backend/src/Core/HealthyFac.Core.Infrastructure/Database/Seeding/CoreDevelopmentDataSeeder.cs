using System.Globalization;
using Bogus;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Companies.Seeding;
using HealthyFac.Core.Infrastructure.Employees.Seeding;
using HealthyFac.Core.Infrastructure.Machines.Seeding;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HealthyFac.Core.Infrastructure.Database.Seeding;

/// <summary>
/// Generates development data from the feature fakers: companies, machines, employees, and open
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
/// <param name="outbox">Queues a <see cref="MachineCreated"/> message for each seeded machine.</param>
public sealed class CoreDevelopmentDataSeeder(
    CoreDbContext dbContext,
    TimeProvider timeProvider,
    IOptions<SeedingOptions> seedingOptions,
    ILogger<CoreDevelopmentDataSeeder> logger,
    IInviteTokenService inviteTokenService,
    IOutbox outbox
)
{
    private readonly CoreDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<CoreDevelopmentDataSeeder> _logger = logger;

    private readonly DevelopmentDataOptions _options = seedingOptions.Value.DevelopmentData;
    private readonly IInviteTokenService _inviteTokenService = inviteTokenService;
    private readonly IOutbox _outbox = outbox;
    private int _seedOffset;

    /// <summary>
    /// Adds the development data. The generated data enters only when the database holds no company yet, so a
    /// restart does not duplicate it. The test tenant enters when its company does not exist yet. With
    /// <see cref="DevelopmentDataOptions.IsRecreationForced"/>, it first deletes the existing business data, the
    /// outbox messages, and the inbox messages, and then adds everything again. Run it after the required roles
    /// exist.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        if (_options.IsRecreationForced)
        {
            _logger.LogWarning(
                "Forced recreation of development data: deleting all companies, machines, error "
                + "codes and machine state histories (also the deleted ones), employees, invites, drafts, "
                + "outbox messages, and inbox messages.");

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
        var hasCompanies = await _dbContext.Companies.AnyAsync(cancellationToken);
        if (hasCompanies)
        {
            return;
        }

        var roleIds = await _dbContext.Roles.ToDictionaryAsync(role => role.Code, role => role.Id, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var inviteLifetime = TimeSpan.FromDays(_options.InviteLifetimeDays);
        var identityRandomizer = new Randomizer(_options.RandomSeed);

        var companies = new CompanyFaker(_options.GetTimeZoneIds(), NextSeed()).Generate(_options.CompanyCount);
        _dbContext.Companies.AddRange(companies);
        await _dbContext.SaveChangesAsync(cancellationToken);

        foreach (var company in companies)
        {
            AddMachines(company.Id, now);

            AddEmployees(
                EmployeeInviteFaker.WithProfile(
                    company.Id,
                    roleIds[RoleCode.Supervisor],
                    now,
                    inviteLifetime,
                    NextSeed()),
                _options.SupervisorsPerCompany,
                now,
                identityRandomizer);

            AddEmployees(
                EmployeeInviteFaker.WithProfile(company.Id, roleIds[RoleCode.Worker], now, inviteLifetime, NextSeed()),
                _options.WorkersPerCompany,
                now,
                identityRandomizer);

            _dbContext.EmployeeInvites.AddRange(
                EmployeeInviteFaker.Open(company.Id, roleIds[RoleCode.Worker], now, inviteLifetime, NextSeed())
                    .Generate(_options.OpenInvitesPerCompany));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTestTenantAsync(TestTenantOptions tenant, CancellationToken cancellationToken)
    {
        var isCompanySeeded = await _dbContext.Companies.AnyAsync(
            company => company.Id == tenant.CompanyId,
            cancellationToken);
        if (isCompanySeeded)
        {
            return;
        }

        var supervisorRoleId = await _dbContext.Roles
            .Where(role => role.Code == RoleCode.Supervisor)
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var company = new Company { Name = tenant.CompanyName, TimeZoneId = tenant.TimeZoneId };
        _dbContext.Entry(company).Property(entity => entity.Id).CurrentValue = tenant.CompanyId;
        _dbContext.Companies.Add(company);

        AddMachines(tenant.CompanyId, now);
        AddSeedMachines(tenant, now);

        var invite = new EmployeeInvite(
            tenant.CompanyId,
            tenant.SupervisorEmail,
            supervisorRoleId,
            _inviteTokenService.CreateSalt(),
            now.AddDays(_options.InviteLifetimeDays));
        invite.UpdateProfile(tenant.SupervisorFirstName, tenant.SupervisorLastName);

        var supervisor = invite.Accept(now);
        _dbContext.Entry(supervisor).Property(entity => entity.Id).CurrentValue = tenant.SupervisorEmployeeId;
        supervisor.Activate(tenant.SupervisorIdentityId);

        _dbContext.EmployeeInvites.Add(invite);
        _dbContext.Employees.Add(supervisor);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteBusinessDataAsync(CancellationToken cancellationToken)
    {
        await _dbContext.OutboxMessages.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.InboxMessages.ExecuteDeleteAsync(cancellationToken);
        await _dbContext.MetricDefinitionDrafts.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.StatusDefinitionDrafts.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.MachineDrafts.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Employees.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.EmployeeInvites.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.MachineStateHistories.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Machines.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
        await _dbContext.Companies.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
            .ExecuteDeleteAsync(cancellationToken);
    }

    private void AddMachines(Guid companyId, DateTime now)
    {
        var machines = new MachineFaker(
                companyId,
                _options.GetMachineTypes(),
                _options.SerialNumberPattern,
                now,
                NextSeed())
            .Generate(_options.MachinesPerCompany);
        _dbContext.Machines.AddRange(machines);
        machines.ForEach(AddMachineCreated);
    }

    private void AddSeedMachines(TestTenantOptions tenant, DateTime now)
    {
        var seeds = _options.GetSeedMachines();

        for (var index = 0; index < seeds.Count; index++)
        {
            var seed = seeds[index];
            var machine = new Machine(
                seed.MachineId,
                tenant.CompanyId,
                seed.GatewayId,
                seed.Name
                ?? string.Format(
                    CultureInfo.InvariantCulture,
                    SeedingValidationConstants.SeedMachineNaming.NameFormat,
                    index + 1),
                string.Format(
                    CultureInfo.InvariantCulture,
                    SeedingValidationConstants.SeedMachineNaming.SerialNumberFormat,
                    index + 1),
                description: null,
                now.AddDays(-SeedingValidationConstants.StateHistory.MinStartDaysBeforeNow));
            _dbContext.Machines.Add(machine);
            AddMachineCreated(machine);
        }
    }

    private void AddMachineCreated(Machine machine) =>
        _outbox.Add(
            new MachineCreated
            {
                MachineId = machine.Id,
                GatewayId = machine.GatewayId,
                CompanyId = machine.CompanyId,
                Name = machine.Name,
            });

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

            _dbContext.EmployeeInvites.Add(invite);
            _dbContext.Employees.Add(employee);
        }
    }

    private int NextSeed() => _options.RandomSeed + _seedOffset++;
}
