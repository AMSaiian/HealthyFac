using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Application.Shifts.DaySchedules;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.ApplySchedule;

/// <summary>
/// The target state of one work order on the schedule board: the assignee and the start together (or both
/// <see langword="null"/> for the backlog) and the estimate.
/// </summary>
public sealed record ScheduleChange
{
    public required Guid WorkOrderId { get; init; }

    /// <summary>The row version that the client read. A stale one ends in the conflict (409).</summary>
    public required uint RowVersion { get; init; }

    public Guid? AssigneeId { get; init; }

    /// <summary>The planned start as a time in the company zone.</summary>
    public DateTime? PlannedStartLocal { get; init; }

    public required int EstimateMinutes { get; init; }
}

public record ApplyScheduleChangesRequest : IRequest<ApplyScheduleChangesResponse>
{
    public required Guid CompanyId { get; init; }

    public required IReadOnlyList<ScheduleChange> Changes { get; init; }
}

public record AppliedWorkOrder
{
    public required Guid Id { get; init; }

    public required uint RowVersion { get; init; }
}

public record ApplyScheduleChangesResponse
{
    public required IReadOnlyList<AppliedWorkOrder> WorkOrders { get; init; }
}

public class ApplyScheduleChangesRequestValidator : AbstractValidator<ApplyScheduleChangesRequest>
{
    public ApplyScheduleChangesRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Changes)
            .Must(changes => changes.Count is >= 1 and <= WorkOrderValidationConstants.ApplyMaxChanges)
            .WithMessage(WorkOrderErrors.Validation.ApplyChangeCountInvalid);

        RuleFor(request => request.Changes)
            .Must(changes => changes.Select(change => change.WorkOrderId).Distinct().Count() == changes.Count)
            .WithMessage(WorkOrderErrors.Validation.ApplyWorkOrderRepeated);

        RuleForEach(request => request.Changes)
            .ChildRules(change =>
            {
                change.RuleFor(item => item.EstimateMinutes).IsWorkOrderEstimate();

                change.RuleFor(item => item)
                    .Must(item => item.AssigneeId is null == item.PlannedStartLocal is null)
                    .WithMessage(WorkOrderErrors.Validation.AssigneeAndStartTogether);
            });
    }
}

public sealed class ApplyScheduleChangesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ApplyScheduleChangesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ApplyScheduleChangesRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Applies the changes of the schedule board in one serializable transaction. The handler builds the final state of
/// every touched assignee (the changed work orders take their new values, the others keep theirs) and checks each
/// change against it, so a swap of two work orders between two employees in the same slot passes. It collects every
/// failure and throws one <see cref="UnprocessableException"/> with the extension <c>changes</c>; then nothing is
/// stored. Otherwise each changed work order adds one history row, and one save stores everything.
/// </summary>
/// <remarks>
/// The handler loads the changed work orders in the order of their IDs, so two applies that share work orders lock the
/// rows in one order. A completed work order is never editable. A resize of a started work order skips the range check
/// of the start, because the start does not change.
/// </remarks>
/// <param name="dbContext">Loads and saves the work orders.</param>
/// <param name="scheduleChecks">Holds the start rules of the single-change paths.</param>
/// <param name="reader">Loads the shifts of the assignees and the zone.</param>
/// <param name="occupancyReader">Reads the occupancies of the assignees.</param>
/// <param name="notifier">Pushes the notifications after the save.</param>
public sealed class ApplyScheduleChangesRequestHandler(
    ICoreDbContext dbContext,
    ScheduleChecks scheduleChecks,
    DayScheduleReader reader,
    EmployeeOccupancyReader occupancyReader,
    IRealtimeNotifier notifier
) : IRequestHandler<ApplyScheduleChangesRequest, ApplyScheduleChangesResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ScheduleChecks _scheduleChecks = scheduleChecks;
    private readonly DayScheduleReader _reader = reader;
    private readonly EmployeeOccupancyReader _occupancyReader = occupancyReader;
    private readonly IRealtimeNotifier _notifier = notifier;

    public async Task<ApplyScheduleChangesResponse> HandleAsync(
        ApplyScheduleChangesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var applied = await _dbContext.RunInTransactionAsync(
            token => ApplyAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
        foreach (var notification in applied.Notifications)
        {
            _notifier.AddScheduleChange(request.CompanyId, notification.Assignment, notification.Estimate);
        }

        _notifier.Push();

        return new ApplyScheduleChangesResponse { WorkOrders = applied.WorkOrders };
    }

    private async Task<Applied> ApplyAsync(ApplyScheduleChangesRequest request, CancellationToken cancellationToken)
    {
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var targets = await LoadTargetsAsync(request, zone, cancellationToken);
        var failures = await FindFailuresAsync(request.CompanyId, targets, zone, cancellationToken);
        if (failures.Count > 0)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.ScheduleChangesRejected)
            {
                Extensions = new Dictionary<string, object?> { [key: "changes"] = failures },
            };
        }

        var notifications = new List<Notification>();
        foreach (var target in targets)
        {
            var oldEstimate = target.WorkOrder.Estimate;
            var oldAssigneeId = target.WorkOrder.Current.AssigneeId;
            var previousRevision = target.WorkOrder.CurrentRevision;
            var scheduleChange = target.WorkOrder.Reschedule(target.Slot, target.Estimate);
            _dbContext.AddNewHistoryRows(target.WorkOrder, previousRevision);
            _dbContext.Entry(target.WorkOrder).State = EntityState.Modified;
            var assignment = await _dbContext.BuildAssignmentChangeAsync(
                target.WorkOrder, scheduleChange, oldAssigneeId, target.Slot, cancellationToken);
            var assigneeUserId = target.Slot is { } slot && target.Estimate != oldEstimate
                ? await _dbContext.FindIdentityUserIdAsync(slot.AssigneeId, cancellationToken)
                : null;
            notifications.Add(
                new Notification
                {
                    Assignment = assignment,
                    Estimate = new WorkOrderEstimateChange
                    {
                        WorkOrderId = target.WorkOrder.Id,
                        Title = target.WorkOrder.Title,
                        AssigneeUserId = assigneeUserId,
                        OldEstimate = oldEstimate,
                        NewEstimate = target.Estimate,
                    },
                });
        }

        await SaveAsync(cancellationToken);

        return new Applied
        {
            Notifications = notifications,
            WorkOrders = targets
                .Select(target => new AppliedWorkOrder
                {
                    Id = target.WorkOrder.Id,
                    RowVersion = target.WorkOrder.RowVersion,
                })
                .ToList(),
        };
    }

    private async Task<List<Target>> LoadTargetsAsync(
        ApplyScheduleChangesRequest request,
        TimeZoneInfo zone,
        CancellationToken cancellationToken
    )
    {
        var ids = request.Changes.Select(change => change.WorkOrderId).Order().ToList();
        var workOrders = await _dbContext.WorkOrders
            .Where(candidate => ids.Contains(candidate.Id) && candidate.CompanyId == request.CompanyId)
            .OrderBy(candidate => candidate.Id)
            .ToListAsync(cancellationToken);
        if (workOrders.Count != ids.Count)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);
        }

        await (from row in _dbContext.WorkOrderHistories
                join workOrder in _dbContext.WorkOrders on row.WorkOrderId equals workOrder.Id
                where ids.Contains(workOrder.Id) && row.Revision == workOrder.CurrentRevision
                orderby row.WorkOrderId
                select row)
            .LoadAsync(cancellationToken);

        var targets = new List<Target>();
        foreach (var change in request.Changes)
        {
            var workOrder = workOrders.Single(candidate => candidate.Id == change.WorkOrderId);
            if (workOrder.RowVersion != change.RowVersion)
            {
                throw new ConflictException(WorkOrderErrors.Rejections.WorkOrderChanged);
            }

            _dbContext.Entry(workOrder).Property(stored => stored.RowVersion).OriginalValue = change.RowVersion;
            targets.Add(Target.Of(change, workOrder, _scheduleChecks, zone));
        }

        return targets;
    }

    private async Task<List<ScheduleChangeFailure>> FindFailuresAsync(
        Guid companyId,
        IReadOnlyList<Target> targets,
        TimeZoneInfo zone,
        CancellationToken cancellationToken
    )
    {
        var changedIds = targets.Select(target => target.WorkOrder.Id).ToHashSet();
        var slotTargets = targets.Where(target => target.Slot is not null).ToList();
        var assigneeIds = slotTargets.Select(target => target.Slot!.AssigneeId).Distinct().Order().ToList();
        var statuses = new Dictionary<Guid, EmployeeStatus>();
        var shiftsByAssignee = new Dictionary<Guid, EmployeeShifts>();
        var occupanciesByAssignee = new Dictionary<Guid, List<Occupancy>>();
        foreach (var assigneeId in assigneeIds)
        {
            var assigneeTargets = slotTargets.Where(target => target.Slot!.AssigneeId == assigneeId).ToList();
            var status = await _scheduleChecks.FindAssigneeStatusAsync(companyId, assigneeId, cancellationToken)
                ?? throw new NotFoundException(WorkOrderErrors.Rejections.AssigneeNotFound);
            statuses[assigneeId] = status;

            var dates = assigneeTargets.Select(target => DateOnly.FromDateTime(target.Slot!.PlannedStartLocal))
                .ToList();
            shiftsByAssignee[assigneeId] = await _reader.LoadAsync(
                assigneeId, zone, dates.Min(), dates.Max(), isTracked: false, cancellationToken);

            var stored = await _occupancyReader.ReadAsync(
                companyId,
                assigneeId,
                assigneeTargets.Min(target => target.Slot!.PlannedStartUtc),
                assigneeTargets.Max(target => target.Slot!.PlannedStartUtc.Add(target.Estimate)),
                excludeWorkOrderId: null,
                cancellationToken);
            occupanciesByAssignee[assigneeId] = stored
                .Where(occupancy => !changedIds.Contains(occupancy.WorkOrderId))
                .Concat(assigneeTargets.Select(target => target.OccupancyOf(target.Slot!)))
                .ToList();
        }

        var failures = new List<ScheduleChangeFailure>();
        foreach (var target in targets)
        {
            var reason = FindReason(target, statuses, shiftsByAssignee, occupanciesByAssignee, zone);
            if (reason is not null)
            {
                failures.Add(new ScheduleChangeFailure { WorkOrderId = target.WorkOrder.Id, Reason = reason });
            }
        }

        return failures;
    }

    private ScheduleChangeRejection? FindReason(
        Target target,
        Dictionary<Guid, EmployeeStatus> statuses,
        Dictionary<Guid, EmployeeShifts> shiftsByAssignee,
        Dictionary<Guid, List<Occupancy>> occupanciesByAssignee,
        TimeZoneInfo zone
    )
    {
        if (target.WorkOrder.Current.Status == WorkOrderStatus.Completed)
        {
            return ScheduleChangeRejection.NotEditable;
        }

        if (target.Slot is not { } slot)
        {
            return null;
        }

        if (statuses[slot.AssigneeId] != EmployeeStatus.Active)
        {
            return ScheduleChangeRejection.AssigneeNotActive;
        }

        var isSlotMoved = target.WorkOrder.Current.AssigneeId != slot.AssigneeId
            || target.WorkOrder.Current.PlannedStartLocal != slot.PlannedStartLocal;
        if (isSlotMoved && !_scheduleChecks.IsStartInRange(slot.PlannedStartLocal, slot.PlannedStartUtc, zone))
        {
            return ScheduleChangeRejection.StartOutOfRange;
        }

        var date = DateOnly.FromDateTime(slot.PlannedStartLocal);
        var occupancy = target.OccupancyOf(slot);
        if (!SlotRules.FitsOneBlock(occupancy, shiftsByAssignee[slot.AssigneeId].Resolve(date, date).Single()))
        {
            return ScheduleChangeRejection.OutsideShift;
        }

        var others = occupanciesByAssignee[slot.AssigneeId]
            .Where(candidate => candidate.WorkOrderId != target.WorkOrder.Id);

        return SlotRules.Overlaps(occupancy, others) ? ScheduleChangeRejection.SlotTaken : null;
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or UniqueConstraintException)
        {
            throw new ConflictException(WorkOrderErrors.Rejections.WorkOrderChanged, exception);
        }
    }

    private sealed class Target
    {
        public required WorkOrder WorkOrder { get; init; }

        public required WorkOrderSlot? Slot { get; init; }

        public required TimeSpan Estimate { get; init; }

        public Occupancy OccupancyOf(WorkOrderSlot slot) =>
            Occupancy.Of(WorkOrder.Id, slot.PlannedStartUtc, Estimate, completedAtUtc: null);

        public static Target Of(
            ScheduleChange change,
            WorkOrder workOrder,
            ScheduleChecks scheduleChecks,
            TimeZoneInfo zone
        ) =>
            new()
            {
                WorkOrder = workOrder,
                Estimate = TimeSpan.FromMinutes(change.EstimateMinutes),
                Slot = change.AssigneeId is { } assigneeId && change.PlannedStartLocal is { } startLocal
                    ? new WorkOrderSlot
                    {
                        AssigneeId = assigneeId,
                        PlannedStartLocal = startLocal,
                        PlannedStartUtc = scheduleChecks.ToStartUtc(startLocal, zone),
                    }
                    : null,
            };
    }

    private sealed class Notification
    {
        public required WorkOrderAssignmentChange? Assignment { get; init; }

        public required WorkOrderEstimateChange Estimate { get; init; }
    }

    private sealed class Applied
    {
        public required IReadOnlyList<Notification> Notifications { get; init; }

        public required IReadOnlyList<AppliedWorkOrder> WorkOrders { get; init; }
    }
}
