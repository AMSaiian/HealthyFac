using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record UpdateWorkOrderRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }

    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public Guid? MachineId { get; init; }

    public required int EstimateMinutes { get; init; }

    public required IReadOnlyList<Guid> FileIds { get; init; }

    /// <summary>
    /// The assignee after the save, or <see langword="null"/> for none. It goes with <see cref="PlannedStartLocal"/>.
    /// </summary>
    public Guid? AssigneeId { get; init; }

    /// <summary>The planned start as a time in the company zone, or <see langword="null"/> for none.</summary>
    public DateTime? PlannedStartLocal { get; init; }

    /// <summary>The row version that the client read. The save fails when the work order changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class UpdateWorkOrderRequestValidator : AbstractValidator<UpdateWorkOrderRequest>
{
    public UpdateWorkOrderRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);

        RuleFor(request => request.Priority).NotNull().WithMessage(WorkOrderErrors.Validation.PriorityRequired);

        RuleFor(request => request.Category).NotNull().WithMessage(WorkOrderErrors.Validation.CategoryRequired);

        RuleFor(request => request.Title).IsWorkOrderTitle();

        RuleFor(request => request.Description).IsWorkOrderDescription(richTextPolicy);

        RuleFor(request => request.EstimateMinutes).IsWorkOrderEstimate();

        RuleFor(request => request.FileIds).IsWorkOrderFileList();

        RuleFor(request => request)
            .Must(request => request.AssigneeId is null == request.PlannedStartLocal is null)
            .WithMessage(WorkOrderErrors.Validation.AssigneeAndStartTogether);
    }
}

public sealed class UpdateWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UpdateWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(UpdateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Changes the data of a work order that the access scope reaches: title, description, priority, category, machine,
/// files, estimate, and the schedule (assignee and start). A schedule change or a new estimate of a scheduled work
/// order runs the schedule rules (assignee, grid and range, work block, overlap) in one serializable transaction, and
/// adds one history row with all new values; every other change runs in one transaction with the default isolation.
/// The own scope may not change the estimate or the schedule. The handler sets the original row version from the
/// request and marks the work order modified, so a change of the files alone also checks and changes <c>xmin</c>. A
/// stale row version ends in the conflict (409).
/// </summary>
/// <remarks>
/// The operation saves twice: the first save holds only the work order row (always an <c>UPDATE</c> with the
/// <c>xmin</c> check), the second holds the file link removals and inserts. EF Core sorts <c>work_order_files</c>
/// before <c>work_orders</c> and orders new links by their Guid v7 keys, which are random within one millisecond. Two
/// updates of one work order could then insert the same links in opposite orders and deadlock on the unique index
/// <c>(work_order_id, file_id)</c>. The first save makes the second update wait on the work order row first.
/// </remarks>
/// <param name="dbContext">Loads and saves the work order.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
/// <param name="scheduleChecks">Checks the slot of a new schedule or estimate.</param>
/// <param name="currentUser">Reads the actor.</param>
public sealed class UpdateWorkOrderRequestHandler(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ScheduleChecks scheduleChecks,
    ICurrentUserProvider currentUser,
    IRealtimeNotifier notifier
) : RequestHandler<UpdateWorkOrderRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ScheduleChecks _scheduleChecks = scheduleChecks;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override async Task HandleInternalAsync(
        UpdateWorkOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var estimate = TimeSpan.FromMinutes(request.EstimateMinutes);
        var stored = await (from row in _dbContext.WorkOrderHistories.AsNoTracking()
                join workOrder in _dbContext.WorkOrders.AsNoTracking() on row.WorkOrderId equals workOrder.Id
                where workOrder.Id == request.Id
                    && workOrder.CompanyId == request.CompanyId
                    && row.Revision == workOrder.CurrentRevision
                select new { workOrder.Estimate, row.AssigneeId, row.PlannedStartLocal })
            .SingleOrDefaultAsync(cancellationToken);
        var isScheduleCheckNeeded = request.Scope == AccessScope.Company
            && stored is not null
            && ((stored.AssigneeId is not null && stored.Estimate != estimate)
                || request.AssigneeId != stored.AssigneeId
                || request.PlannedStartLocal != stored.PlannedStartLocal);

        var change = isScheduleCheckNeeded
            ? await _dbContext.RunInTransactionAsync(
                token => UpdateAsync(request, token),
                IsolationLevel.Serializable,
                cancellationToken)
            : await _dbContext.RunInTransactionAsync(token => UpdateAsync(request, token), cancellationToken);
        _notifier.PushScheduleChange(request.CompanyId, change.Assignment, change.Estimate);
    }

    private async Task<UpdateChange> UpdateAsync(UpdateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;
        var workOrder = await _dbContext.WorkOrders
                .Include(candidate => candidate.Files.OrderBy(link => link.Id))
                .ApplyScope(request.Scope, request.CompanyId, ownEmployeeId, _currentUser.GetUserId())
                .SingleOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);

        await _dbContext.WorkOrderHistories
            .Where(row => row.WorkOrderId == workOrder.Id && row.Revision == workOrder.CurrentRevision)
            .LoadAsync(cancellationToken);
        await EnsureMachineAsync(request, workOrder, cancellationToken);

        var estimate = TimeSpan.FromMinutes(request.EstimateMinutes);
        var oldEstimate = workOrder.Estimate;
        var oldRow = workOrder.Current;
        var oldAssigneeId = oldRow.AssigneeId;
        var slot = await EnsureScheduleAsync(request, workOrder, estimate, cancellationToken);

        var previousRevision = workOrder.CurrentRevision;
        var scheduleChange = workOrder.Reschedule(slot, estimate);
        _dbContext.AddNewHistoryRows(workOrder, previousRevision);

        _dbContext.Entry(workOrder).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        workOrder.Update(request.Title, request.Description, request.Priority, request.Category, request.MachineId);
        _dbContext.Entry(workOrder).State = EntityState.Modified;
        await SaveAsync(cancellationToken);

        workOrder.ReplaceFiles(request.FileIds);
        _dbContext.WorkOrderFiles.AddRange(
            workOrder.Files.Where(link => _dbContext.Entry(link).State == EntityState.Detached));
        await SaveAsync(cancellationToken);

        var assigneeId = workOrder.Current.AssigneeId;
        var assigneeUserId = assigneeId is { } assignee && estimate != oldEstimate
            ? await _dbContext.FindIdentityUserIdAsync(assignee, cancellationToken)
            : null;
        var assignment = await _dbContext.BuildAssignmentChangeAsync(
            workOrder,
            scheduleChange,
            oldAssigneeId,
            slot,
            cancellationToken);

        return new UpdateChange
        {
            Estimate = new WorkOrderEstimateChange
            {
                WorkOrderId = workOrder.Id,
                Title = workOrder.Title,
                AssigneeUserId = assigneeUserId,
                OldEstimate = oldEstimate,
                NewEstimate = estimate,
            },
            Assignment = assignment,
        };
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

    private async Task<WorkOrderSlot?> EnsureScheduleAsync(
        UpdateWorkOrderRequest request,
        WorkOrder workOrder,
        TimeSpan estimate,
        CancellationToken cancellationToken
    )
    {
        var current = workOrder.Current;
        var isSlotChanged = request.AssigneeId != current.AssigneeId
            || request.PlannedStartLocal != current.PlannedStartLocal;
        var isEstimateChanged = estimate != workOrder.Estimate;
        if (request.Scope == AccessScope.Own && (isSlotChanged || isEstimateChanged))
        {
            throw new ForbiddenAccessException(WorkOrderErrors.Rejections.OwnScopeForbidden);
        }

        if (request.AssigneeId is not { } assigneeId || request.PlannedStartLocal is not { } startLocal)
        {
            return null;
        }

        var startUtc = isSlotChanged
            ? await _scheduleChecks.EnsureAsync(
                request.CompanyId,
                assigneeId,
                startLocal,
                estimate,
                workOrder.Id,
                cancellationToken)
            : current.PlannedStartUtc!.Value;
        if (!isSlotChanged && isEstimateChanged)
        {
            await _scheduleChecks.EnsureSlotAsync(
                request.CompanyId,
                assigneeId,
                startLocal,
                startUtc,
                estimate,
                workOrder.Id,
                cancellationToken);
        }

        return new WorkOrderSlot
            { AssigneeId = assigneeId, PlannedStartLocal = startLocal, PlannedStartUtc = startUtc };
    }

    private async Task EnsureMachineAsync(
        UpdateWorkOrderRequest request,
        WorkOrder workOrder,
        CancellationToken cancellationToken
    )
    {
        if (request.MachineId is not { } machineId || machineId == workOrder.MachineId)
        {
            return;
        }

        var isMachineInCompany = await _dbContext.Machines
            .AsNoTracking()
            .AnyAsync(machine => machine.Id == machineId && machine.CompanyId == request.CompanyId, cancellationToken);
        if (!isMachineInCompany)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.MachineNotFound);
        }
    }

    private sealed record UpdateChange
    {
        public required WorkOrderEstimateChange Estimate { get; init; }

        public WorkOrderAssignmentChange? Assignment { get; init; }
    }
}
