using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Shifts;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Core.Domain.WorkOrders.Scheduling;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Time;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record ChangeWorkOrderStatusRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The new status. It is never backlog: an unassign sends a work order back to the backlog.</summary>
    public required WorkOrderStatus Status { get; init; }

    /// <summary>The row version that the client read. The save fails when the work order changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class ChangeWorkOrderStatusRequestValidator : AbstractValidator<ChangeWorkOrderStatusRequest>
{
    public ChangeWorkOrderStatusRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);

        RuleFor(request => request.Status)
            .NotNull()
            .WithMessage(WorkOrderErrors.Validation.StatusRequired)
            .NotEqual(WorkOrderStatus.Backlog)
            .WithMessage(WorkOrderErrors.Validation.StatusIsBacklog);
    }
}

public sealed class ChangeWorkOrderStatusPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ChangeWorkOrderStatusRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ChangeWorkOrderStatusRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Changes the status of a scheduled work order: the new history row copies the assignee and the planned start. Any
/// status but completed is one plain save. A completion runs in one serializable transaction: the later work orders of
/// the same assignee on the same local date slide when the completion is late (<see cref="CompletionSlide"/>), and the
/// completion and every move are saved together. A stale row version, or a parallel change that takes the same
/// revision, ends in the conflict (409).
/// </summary>
/// <remarks>
/// The completion moment is the time of the use case. Each moved work order gets a new history row and a new
/// <c>xmin</c>, so a client that edits it with the old row version gets the conflict.
/// </remarks>
/// <param name="dbContext">Loads and saves the work orders.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
/// <param name="reader">Loads the shifts of the assignee for the slide.</param>
/// <param name="currentUser">Reads the actor.</param>
/// <param name="timeProvider">Provides the completion moment.</param>
public sealed class ChangeWorkOrderStatusRequestHandler(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    DayScheduleReader reader,
    ICurrentUserProvider currentUser,
    TimeProvider timeProvider
) : RequestHandler<ChangeWorkOrderStatusRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly DayScheduleReader _reader = reader;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        ChangeWorkOrderStatusRequest request,
        CancellationToken cancellationToken
    )
    {
        if (request.Status == WorkOrderStatus.Completed)
        {
            await _dbContext.RunInTransactionAsync(
                token => ChangeAsync(request, token),
                IsolationLevel.Serializable,
                cancellationToken);

            return;
        }

        await ChangeAsync(request, cancellationToken);
    }

    private async Task ChangeAsync(ChangeWorkOrderStatusRequest request, CancellationToken cancellationToken)
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;
        var workOrder = await _dbContext.WorkOrders
                .ApplyScope(request.Scope, request.CompanyId, ownEmployeeId, _currentUser.GetUserId())
                .SingleOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);
        await _dbContext.WorkOrderHistories
            .Where(row => row.WorkOrderId == workOrder.Id && row.Revision == workOrder.CurrentRevision)
            .LoadAsync(cancellationToken);

        var current = workOrder.Current;
        EnsureAllowed(request, current, ownEmployeeId);

        var previousRevision = workOrder.CurrentRevision;
        _dbContext.Entry(workOrder).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        workOrder.ChangeStatus(request.Status);
        var changedOrders = new List<WorkOrder> { workOrder };
        var previousRevisions = new Dictionary<Guid, int> { [workOrder.Id] = previousRevision };

        if (request.Status == WorkOrderStatus.Completed)
        {
            await SlideAsync(request, workOrder, current, changedOrders, previousRevisions, cancellationToken);
        }

        foreach (var changedOrder in changedOrders)
        {
            _dbContext.WorkOrderHistories.AddRange(
                changedOrder.History.Where(row => row.Revision > previousRevisions[changedOrder.Id]));
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or UniqueConstraintException)
        {
            throw new ConflictException(WorkOrderErrors.Rejections.WorkOrderChanged, exception);
        }
    }

    private static void EnsureAllowed(
        ChangeWorkOrderStatusRequest request,
        WorkOrderHistory current,
        Guid? ownEmployeeId
    )
    {
        if (request.Scope == AccessScope.Own && current.AssigneeId != ownEmployeeId)
        {
            throw new ForbiddenAccessException(WorkOrderErrors.Rejections.OwnScopeForbidden);
        }

        if (current.Status == WorkOrderStatus.Backlog)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.NotScheduled);
        }

        if (current.Status == request.Status)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.SameStatus);
        }
    }

    private async Task SlideAsync(
        ChangeWorkOrderStatusRequest request,
        WorkOrder completed,
        WorkOrderHistory previous,
        List<WorkOrder> changedOrders,
        Dictionary<Guid, int> previousRevisions,
        CancellationToken cancellationToken
    )
    {
        var assigneeId = previous.AssigneeId!.Value;
        var zone = await _reader.GetZoneAsync(request.CompanyId, cancellationToken);
        var completedAtUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var date = DateOnly.FromDateTime(previous.PlannedStartLocal!.Value);
        var dayStartUtc = LocalTimes.ToUtc(date.ToDateTime(TimeOnly.MinValue), zone);
        var dayEndUtc = LocalTimes.ToUtc(date.AddDays(value: 1).ToDateTime(TimeOnly.MinValue), zone);

        var others = await _dbContext.WorkOrders
            .Where(candidate => candidate.CompanyId == request.CompanyId
                && candidate.Id != completed.Id
                && candidate.History.Any(row => row.Revision == candidate.CurrentRevision
                    && row.AssigneeId == assigneeId
                    && row.Status != WorkOrderStatus.Backlog
                    && row.PlannedStartUtc >= dayStartUtc
                    && row.PlannedStartUtc < dayEndUtc))
            .OrderBy(candidate => candidate.Id)
            .ToListAsync(cancellationToken);
        var otherIds = others.Select(order => order.Id).ToList();
        await (from row in _dbContext.WorkOrderHistories
                join order in _dbContext.WorkOrders on row.WorkOrderId equals order.Id
                where otherIds.Contains(order.Id) && row.Revision == order.CurrentRevision
                select row)
            .LoadAsync(cancellationToken);

        var shifts = await _reader.LoadAsync(assigneeId, zone, date, date, isTracked: false, cancellationToken);
        var moves = CompletionSlide.Plan(
            new SlideWorkOrder
            {
                Id = completed.Id,
                Status = WorkOrderStatus.Completed,
                PlannedStartUtc = previous.PlannedStartUtc!.Value,
                Estimate = completed.Estimate,
            },
            completedAtUtc,
            others.Select(ToSlideWorkOrder).ToList(),
            shifts.Resolve(date, date).Single(),
            zone);

        foreach (var move in moves)
        {
            var order = others.Single(candidate => candidate.Id == move.WorkOrderId);
            previousRevisions[order.Id] = order.CurrentRevision;
            if (move.NewStartUtc is { } newStartUtc)
            {
                order.Move(
                    assigneeId,
                    TimeZoneInfo.ConvertTimeFromUtc(newStartUtc, zone),
                    newStartUtc);
            }
            else
            {
                order.Unassign();
            }

            changedOrders.Add(order);
        }
    }

    private static SlideWorkOrder ToSlideWorkOrder(WorkOrder order) =>
        new()
        {
            Id = order.Id,
            Status = order.Current.Status,
            PlannedStartUtc = order.Current.PlannedStartUtc!.Value,
            Estimate = order.Estimate,
            CompletedAtUtc = order.Current.Status == WorkOrderStatus.Completed ? order.Current.CreatedAt : null,
        };
}
