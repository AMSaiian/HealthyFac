using System.Globalization;
using Bogus;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Machines;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Domain.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Infrastructure.Companies.Seeding;
using HealthyFac.Core.Infrastructure.Employees.Seeding;
using HealthyFac.Core.Infrastructure.Machines.Seeding;
using HealthyFac.Core.Infrastructure.WorkOrders.Seeding;
using HealthyFac.Core.Messaging.Machines;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Domain.Entities;
using HealthyFac.Shared.Domain.Time;
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

        var roleIds = await _dbContext.Roles.ToDictionaryAsync(role => role.Code, role => role.Id, cancellationToken);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var company = new Company { Name = tenant.CompanyName, TimeZoneId = tenant.TimeZoneId };
        _dbContext.Entry(company).Property(entity => entity.Id).CurrentValue = tenant.CompanyId;
        _dbContext.Companies.Add(company);

        AddMachines(tenant.CompanyId, now);
        AddSeedMachines(tenant, now);

        var supervisor = AddTestTenantEmployee(
            tenant,
            roleIds[RoleCode.Supervisor],
            new TestTenantEmployee
            {
                Id = tenant.SupervisorEmployeeId,
                IdentityId = tenant.SupervisorIdentityId,
                Email = tenant.SupervisorEmail,
                FirstName = tenant.SupervisorFirstName,
                LastName = tenant.SupervisorLastName,
            },
            now);
        var worker = tenant.Worker is null
            ? supervisor
            : AddTestTenantEmployee(
                tenant,
                roleIds[RoleCode.Worker],
                new TestTenantEmployee
                {
                    Id = tenant.Worker.EmployeeId,
                    IdentityId = tenant.Worker.IdentityId,
                    Email = tenant.Worker.Email,
                    FirstName = tenant.Worker.FirstName,
                    LastName = tenant.Worker.LastName,
                },
                now);

        AddTestTenantSchedule(tenant, supervisor, worker, now);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private Employee AddTestTenantEmployee(
        TestTenantOptions tenant,
        Guid roleId,
        TestTenantEmployee person,
        DateTime now
    )
    {
        var invite = new EmployeeInvite(
            tenant.CompanyId,
            person.Email,
            roleId,
            _inviteTokenService.CreateSalt(),
            now.AddDays(_options.InviteLifetimeDays));
        invite.UpdateProfile(person.FirstName, person.LastName);

        var employee = invite.Accept(now);
        _dbContext.Entry(employee).Property(entity => entity.Id).CurrentValue = person.Id;
        employee.Activate(person.IdentityId);

        _dbContext.EmployeeInvites.Add(invite);
        _dbContext.Employees.Add(employee);

        return employee;
    }

    private void AddTestTenantSchedule(TestTenantOptions tenant, Employee supervisor, Employee worker, DateTime now)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, zone));
        var firstDay = NextWorkday(today);
        var secondDay = NextWorkday(firstDay.AddDays(value: 1));

        foreach (var employee in new[] { supervisor, worker }.DistinctBy(employee => employee.Id))
        {
            _dbContext.ShiftTemplateVersions.Add(
                new ShiftTemplateVersion(
                    employee.Id,
                    today,
                    SeedSchedule.Workdays.SelectMany(weekday =>
                        SeedSchedule.Blocks.Select(times => (weekday, times)))));
        }

        var machineIds = _options.GetSeedMachines().Select(machine => machine.MachineId).ToList();
        var orders = new WorkOrderFaker(tenant.CompanyId, machineIds, NextSeed()).Generate(count: 6);
        _dbContext.WorkOrders.AddRange(orders);

        Schedule(orders[index: 1], worker, firstDay, SeedSchedule.FirstSlot, zone, WorkOrderStatus.InProgress);
        Schedule(orders[index: 2], worker, firstDay, SeedSchedule.SecondSlot, zone, WorkOrderStatus.Blocked);
        Schedule(orders[index: 3], worker, firstDay, SeedSchedule.ThirdSlot, zone, WorkOrderStatus.Staged);
        Schedule(orders[index: 4], supervisor, firstDay, SeedSchedule.AfternoonSlot, zone, WorkOrderStatus.Completed);
        Schedule(orders[index: 5], supervisor, secondDay, SeedSchedule.AfternoonSlot, zone, WorkOrderStatus.Staged);
    }

    private static void Schedule(
        WorkOrder order,
        Employee assignee,
        DateOnly day,
        TimeOnly time,
        TimeZoneInfo zone,
        WorkOrderStatus status
    )
    {
        var local = day.ToDateTime(time);
        order.Assign(assignee.Id, local, LocalTimes.ToUtc(local, zone));

        if (status != WorkOrderStatus.Staged)
        {
            order.ChangeStatus(status);
        }
    }

    private static DateOnly NextWorkday(DateOnly day)
    {
        while (!SeedSchedule.Workdays.Any(weekday => weekday.DayOfWeek == day.DayOfWeek))
        {
            day = day.AddDays(value: 1);
        }

        return day;
    }

    private Task DeleteBusinessDataAsync(CancellationToken cancellationToken) =>
        _dbContext.RunInTransactionAsync(
            async token =>
            {
                await _dbContext.OutboxMessages.ExecuteDeleteAsync(token);
                await _dbContext.InboxMessages.ExecuteDeleteAsync(token);
                await _dbContext.MetricDefinitionDrafts.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.StatusDefinitionDrafts.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.MachineDrafts.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.WorkOrderFiles.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.WorkOrderHistories.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.WorkOrders.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.ShiftOverrideBlocks.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.ShiftOverrides.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.ShiftTemplateBlocks.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.ShiftTemplateVersions.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.Employees.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.EmployeeInvites.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.MachineStateHistories.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.Machines.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
                await _dbContext.Companies.IgnoreQueryFilters([SoftDeleteConstants.QueryFilterName])
                    .ExecuteDeleteAsync(token);
            },
            cancellationToken);

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
