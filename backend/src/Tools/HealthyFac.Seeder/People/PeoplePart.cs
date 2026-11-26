using HealthyFac.Core.Application.Employees;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Application.Shifts.BlockInput;
using HealthyFac.Core.Domain.Companies;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.Accounts;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.People;

/// <summary>
/// Seeds the company, its people with their Keycloak accounts, the open invites, and the shifts, from
/// <c>company.json</c>. Every step goes through the use cases of Core, so the validators, the messages, and the
/// provisioning run as in the product. Two steps use the domain: the company (no use case creates it) and the invite
/// of the first supervisor (no actor exists yet).
/// </summary>
public sealed class PeoplePart : ISeedPart
{
    private readonly ServiceHosts.ServiceHosts _hosts;
    private readonly KeycloakAccounts _accounts;
    private readonly Dictionary<string, Func<SeedContext, CancellationToken, Task>> _actions = [];

    private CompanyFile _file = null!;

    /// <summary>The time zone of the company. It is set by <see cref="ReadEvents"/>.</summary>
    public string CompanyZoneId => _file.Company.TimeZoneId;

    public PeoplePart(ServiceHosts.ServiceHosts hosts, KeycloakAccounts accounts)
    {
        _hosts = hosts;
        _accounts = accounts;
    }

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder)
    {
        _file = folder.Read<CompanyFile>(PeopleConstants.FileName);
        _actions.Clear();

        var events = new List<SeedEvent>
        {
            Add(
                PeopleConstants.CompanyKey,
                _file.Company.CreatedAt,
                actor: null,
                CreateCompanyAsync,
                [PeopleConstants.CompanyKey]),
        };

        foreach (var person in _file.People)
        {
            events.Add(InviteEvent(person));
            events.Add(AcceptEvent(person));
            events.Add(ShiftEvent(person));
        }

        events.AddRange(_file.OpenInvites.Select(OpenInviteEvent));
        events.AddRange(_file.ShiftOverrides.Select(OverrideEvent));

        return events;
    }

    public Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken) =>
        _actions[SeedItem.KeyOf(seedEvent.Item)](context, cancellationToken);

    private static TimeOnly ShiftTime(PersonSeed person)
    {
        var item = SeedItem.Of(PeopleConstants.FileName, $"{person.Key}{PeopleConstants.AcceptedSuffix}");

        if (person.AcceptedAt.Time is not { } acceptedTime)
        {
            throw new SeedFileException(PeopleErrors.AcceptNeedsTime(item));
        }

        if (acceptedTime.ToTimeSpan() + PeopleConstants.ShiftDelay >= TimeSpan.FromDays(days: 1))
        {
            throw new SeedFileException(PeopleErrors.AcceptTooLate(item));
        }

        return acceptedTime.Add(PeopleConstants.ShiftDelay);
    }

    private static string InviteKey(string key) => $"{key}{PeopleConstants.InviteSuffix}";

    private static string EmployeeKey(string key) => $"{key}{PeopleConstants.EmployeeSuffix}";

    private static string UserKey(string key) => $"{key}{PeopleConstants.UserSuffix}";

    private SeedEvent InviteEvent(PersonSeed person)
    {
        var inviter = person.InvitedBy;

        Func<SeedContext, CancellationToken, Task> action = inviter is null
            ? (context, token) => InviteFirstSupervisorAsync(person, context, token)
            : (context, token) => InviteAsync(person, context, token);

        var uses = inviter is null
            ? new[] { PeopleConstants.CompanyKey }
            : [PeopleConstants.CompanyKey, UserKey(inviter)];

        return Add(
            $"{person.Key}{PeopleConstants.InvitedSuffix}",
            person.InvitedAt,
            inviter,
            action,
            [InviteKey(person.Key)],
            uses,
            inviter is null ? [] : [CoreRequest(InviteRequest(person))]);
    }

    private SeedEvent AcceptEvent(PersonSeed person) =>
        Add(
            $"{person.Key}{PeopleConstants.AcceptedSuffix}",
            person.AcceptedAt,
            actor: null,
            (context, token) => AcceptAsync(person, context, token),
            [EmployeeKey(person.Key), UserKey(person.Key)],
            [InviteKey(person.Key)],
            [CoreRequest(AcceptRequest(person, PeopleConstants.CheckToken))],
            [new SeedCredential { Username = person.Email, Password = _file.Password }]);

    private SeedEvent ShiftEvent(PersonSeed person)
    {
        var actor = person.InvitedBy ?? person.Key;
        var at = person.AcceptedAt with { Time = ShiftTime(person) };

        return Add(
            $"{person.Key}{PeopleConstants.ShiftSuffix}",
            at,
            actor,
            (context, token) => CreateShiftAsync(person, actor, context, token),
            [$"{person.Key}{PeopleConstants.ShiftSuffix}"],
            [EmployeeKey(person.Key), UserKey(actor)],
            [CoreRequest(StandardWeekRequest(PeopleConstants.CheckId, DateOnly.MinValue))]);
    }

    private SeedEvent OpenInviteEvent(OpenInviteSeed invite) =>
        Add(
            $"{invite.Email}{PeopleConstants.InvitedSuffix}",
            invite.InvitedAt,
            invite.InvitedBy,
            (context, token) => InviteOpenAsync(invite, context, token),
            [InviteKey(invite.Email)],
            [PeopleConstants.CompanyKey, UserKey(invite.InvitedBy)],
            [CoreRequest(OpenInviteRequest(invite))]);

    private SeedEvent OverrideEvent(ShiftOverrideSeed shiftOverride) =>
        Add(
            $"{shiftOverride.Person}{PeopleConstants.OverrideSuffix}",
            shiftOverride.SetAt,
            shiftOverride.SetBy,
            (context, token) => SetOverrideAsync(shiftOverride, context, token),
            uses: [EmployeeKey(shiftOverride.Person), UserKey(shiftOverride.SetBy)],
            requests: [CoreRequest(OverrideRequest(shiftOverride, PeopleConstants.CheckId, DateOnly.MinValue))]);

    private SeedEvent Add(
        string key,
        RelativeMoment at,
        string? actor,
        Func<SeedContext, CancellationToken, Task> action,
        string[]? creates = null,
        string[]? uses = null,
        IReadOnlyList<SeedRequest>? requests = null,
        IReadOnlyList<SeedCredential>? credentials = null
    )
    {
        _actions[key] = action;

        return new SeedEvent
        {
            At = at,
            Actor = actor,
            Item = SeedItem.Of(PeopleConstants.FileName, key),
            Creates = creates ?? [],
            Uses = uses ?? [],
            Requests = requests ?? [],
            Credentials = credentials ?? [],
        };
    }

    /// <summary>Saves the company, then creates its Keycloak organization.</summary>
    /// <remarks>
    /// Finish: the organization call after the committed save gets <see cref="CancellationToken.None"/>, so a stop
    /// never leaves a company without its organization. The call is short.
    /// </remarks>
    /// <param name="context">The keys and the actors of the run.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests, until the save.</param>
    /// <returns>A task that completes when the organization exists.</returns>
    private async Task CreateCompanyAsync(SeedContext context, CancellationToken cancellationToken)
    {
        var companySeed = _file.Company;

        using (var scope = _hosts.Core.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core)))
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
            var company = new Company { Name = companySeed.Name, TimeZoneId = companySeed.TimeZoneId };
            dbContext.Companies.Add(company);
            dbContext.Entry(company).Property(candidate => candidate.Id).CurrentValue = companySeed.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await _accounts.CreateOrganizationAsync(companySeed.Id, companySeed.Name, CancellationToken.None);
        context.SetId(PeopleConstants.CompanyKey, companySeed.Id);
    }

    private async Task InviteFirstSupervisorAsync(
        PersonSeed person,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var companyId = _file.Company.Id;
        using var scope = _hosts.Core.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core, companyId));
        var dbContext = scope.ServiceProvider.GetRequiredService<CoreDbContext>();
        var tokens = scope.ServiceProvider.GetRequiredService<IInviteTokenService>();
        var now = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;

        var roleId = await dbContext.Roles
            .Where(role => role.Code == person.Role)
            .Select(role => role.Id)
            .SingleAsync(cancellationToken);

        var invite = new EmployeeInvite(companyId, person.Email, roleId, tokens.CreateSalt(), tokens.GetExpiresAt(now));
        invite.UpdateProfile(person.FirstName, person.LastName);
        dbContext.EmployeeInvites.Add(invite);
        await dbContext.SaveChangesAsync(cancellationToken);
        context.SetId(InviteKey(person.Key), invite.Id);
    }

    private async Task InviteAsync(PersonSeed person, SeedContext context, CancellationToken cancellationToken)
    {
        var response = await _hosts.Core.SendAsync<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse>(
            context.GetActor(person.InvitedBy!),
            InviteRequest(person),
            cancellationToken);

        context.SetId(InviteKey(person.Key), response.Id);
    }

    private async Task InviteOpenAsync(OpenInviteSeed invite, SeedContext context, CancellationToken cancellationToken)
    {
        var response = await _hosts.Core.SendAsync<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse>(
            context.GetActor(invite.InvitedBy),
            OpenInviteRequest(invite),
            cancellationToken);

        context.SetId(InviteKey(invite.Email), response.Id);
    }

    /// <summary>Accepts the invite of a person, then creates the Keycloak user.</summary>
    /// <remarks>
    /// Finish: the user call after the committed accept gets <see cref="CancellationToken.None"/>, so a stop never
    /// leaves an employee without its user. The call is short.
    /// </remarks>
    /// <param name="person">The person.</param>
    /// <param name="context">The keys and the actors of the run.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to observe for cancellation
    /// requests, until the accept.</param>
    /// <returns>A task that completes when the user exists.</returns>
    private async Task AcceptAsync(PersonSeed person, SeedContext context, CancellationToken cancellationToken)
    {
        var inviteId = context.GetId(InviteKey(person.Key));
        var token = await CreateTokenAsync(inviteId, cancellationToken);

        var response = await _hosts.Core.SendAsync<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse>(
            KnownActors.Anonymous,
            AcceptRequest(person, token),
            cancellationToken);

        var userId = await _accounts.CreateUserAsync(
            response.EmployeeId,
            person.Email,
            person.FirstName,
            person.LastName,
            _file.Password,
            CancellationToken.None);

        context.SetId(EmployeeKey(person.Key), response.EmployeeId);
        context.SetId(UserKey(person.Key), userId);

        context.SetActor(
            person.Key,
            new ActorContext
            {
                Kind = ActorKind.User,
                UserId = userId,
                CompanyId = _file.Company.Id,
                EmployeeId = response.EmployeeId,
            });
    }

    private async Task<string> CreateTokenAsync(Guid inviteId, CancellationToken cancellationToken)
    {
        using var scope = _hosts.Core.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core));

        var salt = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
                .EmployeeInvites
                .Where(invite => invite.Id == inviteId)
                .Select(invite => invite.TokenSalt)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException(PeopleErrors.InviteMissing(inviteId));

        return scope.ServiceProvider.GetRequiredService<IInviteTokenService>().CreateToken(inviteId, salt);
    }

    private async Task CreateShiftAsync(
        PersonSeed person,
        string actor,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var effectiveFrom = SeedCalendar.ToDate(person.AcceptedAt, context.Anchor, context.Zone).AddDays(value: 1);

        await _hosts.Core.SendAsync<CreateShiftTemplateVersionRequest, CreateShiftTemplateVersionResponse>(
            context.GetActor(actor),
            StandardWeekRequest(context.GetId(EmployeeKey(person.Key)), effectiveFrom),
            cancellationToken);
    }

    private async Task SetOverrideAsync(
        ShiftOverrideSeed shiftOverride,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var day = SeedCalendar.ToDate(shiftOverride.Day, context.Anchor, context.Zone);

        await _hosts.Core.SendAsync(
            context.GetActor(shiftOverride.SetBy),
            OverrideRequest(shiftOverride, context.GetId(EmployeeKey(shiftOverride.Person)), day),
            cancellationToken);
    }

    private CreateEmployeeInviteRequest InviteRequest(PersonSeed person) =>
        new()
        {
            CompanyId = _file.Company.Id,
            Email = person.Email,
            RoleCode = person.Role,
            FirstName = person.FirstName,
            LastName = person.LastName,
        };

    private CreateEmployeeInviteRequest OpenInviteRequest(OpenInviteSeed invite) =>
        new() { CompanyId = _file.Company.Id, Email = invite.Email, RoleCode = invite.Role };

    private static AcceptEmployeeInviteRequest AcceptRequest(PersonSeed person, string token) =>
        new() { Token = token, FirstName = person.FirstName, LastName = person.LastName };

    private CreateShiftTemplateVersionRequest StandardWeekRequest(Guid employeeId, DateOnly effectiveFrom) =>
        new()
        {
            CompanyId = _file.Company.Id,
            EmployeeId = employeeId,
            EffectiveFrom = effectiveFrom,
            Days =
            [
                .. PeopleConstants.Workdays.Select(weekday => new ShiftTemplateDayInput
                {
                    Weekday = weekday,
                    Blocks = PeopleConstants.StandardBlocks,
                }),
                .. PeopleConstants.Weekend.Select(weekday => new ShiftTemplateDayInput
                {
                    Weekday = weekday,
                    Blocks = [],
                }),
            ],
        };

    private SetShiftOverrideRequest OverrideRequest(ShiftOverrideSeed shiftOverride, Guid employeeId, DateOnly date) =>
        new()
        {
            CompanyId = _file.Company.Id,
            EmployeeId = employeeId,
            Date = date,
            Blocks =
            [
                .. shiftOverride.Blocks.Select(block => new ShiftBlockInput { Start = block.From, End = block.To }),
            ],
        };

    private static SeedRequest CoreRequest(object request) =>
        new() { Service = ServiceName.Core, Request = request };
}
