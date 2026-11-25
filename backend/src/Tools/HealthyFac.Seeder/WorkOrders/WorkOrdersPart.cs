using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.WorkOrders;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Infrastructure.Database;
using HealthyFac.Seeder.Machines;
using HealthyFac.Seeder.Moments;
using HealthyFac.Seeder.People;
using HealthyFac.Seeder.SeedFiles;
using HealthyFac.Seeder.Timeline;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HealthyFac.Seeder.WorkOrders;

/// <summary>
/// Seeds the work orders from <c>work-orders.json</c>. A supervisor creates and assigns each order, and the assignee
/// changes its status, all through the use cases of Core. The scope of an actor follows its role: a supervisor works
/// in the company scope, a worker in the own scope. A rejection of a use case (a shift, a slot, a grid) stops the job
/// with the key of the order.
/// </summary>
public sealed class WorkOrdersPart : ISeedPart
{
    private readonly ServiceHosts.ServiceHosts _hosts;
    private readonly string _fileName;
    private readonly PlannedStartChooser? _chooser;
    private readonly Dictionary<string, Func<SeedContext, CancellationToken, Task>> _actions = [];

    private IReadOnlyDictionary<string, RoleCode> _roles = null!;

    public WorkOrdersPart(ServiceHosts.ServiceHosts hosts)
        : this(hosts, WorkOrdersConstants.FileName)
    {
    }

    /// <summary>Creates a part whose events name another seed file, for the work orders of a script.</summary>
    /// <param name="hosts">The service hosts that the use cases go through.</param>
    /// <param name="fileName">The file that the item names of the events carry.</param>
    /// <param name="chooser">
    /// Chooses the planned start at run time, for the orders whose start depends on the shifts. <see langword="null"/>
    /// takes the planned start of the assignment as it is.
    /// </param>
    internal WorkOrdersPart(ServiceHosts.ServiceHosts hosts, string fileName, PlannedStartChooser? chooser = null)
    {
        _hosts = hosts;
        _fileName = fileName;
        _chooser = chooser;
    }

    public IReadOnlyList<SeedEvent> ReadEvents(SeedFolder folder)
    {
        var file = folder.Read<WorkOrdersFile>(WorkOrdersConstants.FileName);

        return ReadEventsOf(file.WorkOrders, folder.Read<CompanyFile>(PeopleConstants.FileName).People);
    }

    /// <summary>Lists the events of work orders, in the order of the orders.</summary>
    /// <param name="orders">The work orders.</param>
    /// <param name="people">The people of the company, for the scope of each actor.</param>
    /// <returns>The events of the create, the assign, and the status changes of each order.</returns>
    internal IReadOnlyList<SeedEvent> ReadEventsOf(IEnumerable<WorkOrderSeed> orders, IEnumerable<PersonSeed> people)
    {
        _roles = people.ToDictionary(p => p.Key, p => p.Role);
        _actions.Clear();
        var events = new List<SeedEvent>();
        foreach (var order in orders)
        {
            events.Add(CreateEvent(order));
            if (order.Assignment is { } assignment)
            {
                events.Add(AssignEvent(order, assignment));
            }

            events.AddRange(order.StatusChanges.Select((change, index) => StatusEvent(order, change, index)));
        }

        return events;
    }

    public Task RunAsync(SeedEvent seedEvent, SeedContext context, CancellationToken cancellationToken) =>
        _actions[SeedItem.KeyOf(seedEvent.Item)](context, cancellationToken);

    private static string OrderKey(WorkOrderSeed order) => $"{order.Key}{WorkOrdersConstants.OrderSuffix}";

    private static string AssignedKey(WorkOrderSeed order) => $"{order.Key}{WorkOrdersConstants.AssignedSuffix}";

    private static string UserKey(string person) => $"{person}{PeopleConstants.UserSuffix}";

    private static string EmployeeKey(string person) => $"{person}{PeopleConstants.EmployeeSuffix}";

    private static string MachineKey(WorkOrderSeed order) => $"{order.Machine}{MachinesConstants.MachineSuffix}";

    private static SeedRequest CoreRequest(object request) => new() { Service = ServiceName.Core, Request = request };

    private SeedEvent CreateEvent(WorkOrderSeed order)
    {
        var key = $"{order.Key}{WorkOrdersConstants.CreatedSuffix}";
        _actions[key] = (context, cancellationToken) => CreateAsync(order, context, cancellationToken);

        return new SeedEvent
        {
            At = order.CreatedAt,
            Actor = order.CreatedBy,
            Item = SeedItem.Of(_fileName, key),
            Creates = [OrderKey(order)],
            Uses = [PeopleConstants.CompanyKey, MachineKey(order), UserKey(order.CreatedBy)],
            Requests = [CoreRequest(CreateRequest(order, PeopleConstants.CheckId, PeopleConstants.CheckId))],
        };
    }

    private SeedEvent AssignEvent(WorkOrderSeed order, AssignmentSeed assignment)
    {
        var key = AssignedKey(order);
        var item = SeedItem.Of(_fileName, key);
        _actions[key] = (context, cancellationToken) =>
            AssignAsync(order, assignment, item, context, cancellationToken);

        return new SeedEvent
        {
            At = assignment.At,
            Actor = assignment.By,
            Item = item,
            Creates = [key],
            Uses = [OrderKey(order), EmployeeKey(assignment.Assignee), UserKey(assignment.By)],
            Requests =
            [
                CoreRequest(
                    AssignRequest(
                        assignment,
                        item,
                        PeopleConstants.CheckId,
                        PeopleConstants.CheckId,
                        PeopleConstants.CheckId,
                        DateTime.MinValue,
                        rowVersion: 0)),
            ],
        };
    }

    private SeedEvent StatusEvent(WorkOrderSeed order, StatusChangeSeed change, int index)
    {
        var key = $"{order.Key}{WorkOrdersConstants.StatusSuffix}{index}.{change.Status.Value}";
        var item = SeedItem.Of(_fileName, key);
        if (order.Assignment is null)
        {
            throw new SeedFileException(WorkOrdersErrors.StatusNeedsAssignment(item));
        }

        _actions[key] = (context, cancellationToken) =>
            ChangeStatusAsync(order, change, item, context, cancellationToken);

        return new SeedEvent
        {
            At = change.At,
            Actor = change.By,
            Item = item,
            Uses = [AssignedKey(order), UserKey(change.By)],
            Requests =
            [
                CoreRequest(
                    StatusRequest(change, item, PeopleConstants.CheckId, PeopleConstants.CheckId, rowVersion: 0)),
            ],
        };
    }

    private AccessScope ScopeOf(string person, string item) =>
        _roles.TryGetValue(person, out var role)
            ? role == RoleCode.Supervisor ? AccessScope.Company : AccessScope.Own
            : throw new SeedFileException(WorkOrdersErrors.PersonUnknown(item, person));

    private CreateWorkOrderRequest CreateRequest(WorkOrderSeed order, Guid companyId, Guid machineId) =>
        new()
        {
            CompanyId = companyId,
            Scope = ScopeOf(
                order.CreatedBy,
                SeedItem.Of(_fileName, $"{order.Key}{WorkOrdersConstants.CreatedSuffix}")),
            Title = order.Title,
            Description = order.Description,
            Priority = order.Priority,
            Category = order.Category,
            MachineId = machineId,
            EstimateMinutes = order.EstimateMinutes,
            FileIds = [],
        };

    private static AssignWorkOrderRequest AssignRequest(
        AssignmentSeed assignment,
        string item,
        Guid companyId,
        Guid orderId,
        Guid assigneeId,
        DateTime plannedStart,
        uint rowVersion
    ) =>
        new()
        {
            CompanyId = companyId,
            Id = orderId,
            AssigneeId = assigneeId,
            PlannedStartLocal = assignment.PlannedStart is { Time: null, Form: not MomentForm.Hour }
                ? throw new SeedFileException(WorkOrdersErrors.PlannedStartNeedsTime(item))
                : plannedStart,
            RowVersion = rowVersion,
        };

    private ChangeWorkOrderStatusRequest StatusRequest(
        StatusChangeSeed change,
        string item,
        Guid companyId,
        Guid orderId,
        uint rowVersion
    ) =>
        new()
        {
            CompanyId = companyId,
            Scope = ScopeOf(change.By, item),
            Id = orderId,
            Status = change.Status,
            RowVersion = rowVersion,
        };

    private async Task CreateAsync(WorkOrderSeed order, SeedContext context, CancellationToken cancellationToken)
    {
        var response = await _hosts.Core.SendAsync<CreateWorkOrderRequest, CreateWorkOrderResponse>(
            context.GetActor(order.CreatedBy),
            CreateRequest(order, context.GetId(PeopleConstants.CompanyKey), context.GetId(MachineKey(order))),
            cancellationToken);
        context.SetId(OrderKey(order), response.Id);
    }

    private async Task AssignAsync(
        WorkOrderSeed order,
        AssignmentSeed assignment,
        string item,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var orderId = context.GetId(OrderKey(order));
        var assigneeId = context.GetId(EmployeeKey(assignment.Assignee));
        var plannedStart = _chooser is null
            ? PlannedStartLocal(assignment.PlannedStart, context)
            : await _chooser(order, assigneeId, context, cancellationToken);
        var rowVersion = await ReadRowVersionAsync(orderId, cancellationToken);

        await _hosts.Core.SendAsync(
            context.GetActor(assignment.By),
            AssignRequest(
                assignment,
                item,
                context.GetId(PeopleConstants.CompanyKey),
                orderId,
                assigneeId,
                plannedStart,
                rowVersion),
            cancellationToken);
        context.SetId(AssignedKey(order), orderId);
    }

    private async Task ChangeStatusAsync(
        WorkOrderSeed order,
        StatusChangeSeed change,
        string item,
        SeedContext context,
        CancellationToken cancellationToken
    )
    {
        var orderId = context.GetId(OrderKey(order));
        var rowVersion = await ReadRowVersionAsync(orderId, cancellationToken);

        await _hosts.Core.SendAsync(
            context.GetActor(change.By),
            StatusRequest(change, item, context.GetId(PeopleConstants.CompanyKey), orderId, rowVersion),
            cancellationToken);
    }

    private async Task<uint> ReadRowVersionAsync(Guid orderId, CancellationToken cancellationToken)
    {
        using var scope = _hosts.Core.CreateScope(ServiceActor.DatabaseInitialization.For(ServiceName.Core));
        var rowVersion = await scope.ServiceProvider.GetRequiredService<CoreDbContext>()
            .WorkOrders
            .Where(order => order.Id == orderId)
            .Select(order => (uint?)order.RowVersion)
            .SingleOrDefaultAsync(cancellationToken);

        return rowVersion ?? throw new InvalidOperationException(WorkOrdersErrors.OrderMissing(orderId));
    }

    private static DateTime PlannedStartLocal(RelativeMoment moment, SeedContext context) =>
        moment.Form == MomentForm.Hour
            ? TimeZoneInfo.ConvertTime(SeedCalendar.ToUtc(moment, context.Anchor, context.Zone), context.Zone).DateTime
            : SeedCalendar.ToDate(moment, context.Anchor, context.Zone)
                .ToDateTime(moment.Time ?? default(TimeOnly), DateTimeKind.Unspecified);
}
