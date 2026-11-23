using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Realtime;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Realtime;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record MoveWorkOrderRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The new assignee, or <see langword="null"/> to keep the current one.</summary>
    public Guid? AssigneeId { get; init; }

    /// <summary>The new planned start as a time in the company zone.</summary>
    public required DateTime PlannedStartLocal { get; init; }

    /// <summary>The row version that the client read. The save fails when the work order changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class MoveWorkOrderRequestValidator : AbstractValidator<MoveWorkOrderRequest>
{
    public MoveWorkOrderRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);

        RuleFor(request => request.AssigneeId)
            .NotEqual(Guid.Empty)
            .WithMessage(WorkOrderErrors.Validation.AssigneeIdRequired)
            .When(request => request.AssigneeId.HasValue);
    }
}

public sealed class MoveWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<MoveWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(MoveWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Moves a scheduled work order to a new start and, optionally, a new assignee. The four schedule rules run in order,
/// the new history row keeps the status, and the change is one serializable transaction.
/// </summary>
/// <param name="dbContext">Loads and saves the work order.</param>
/// <param name="scheduleChecks">Checks the slot.</param>
public sealed class MoveWorkOrderRequestHandler(
    ICoreDbContext dbContext,
    ScheduleChecks scheduleChecks,
    IRealtimeNotifier notifier
) : RequestHandler<MoveWorkOrderRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ScheduleChecks _scheduleChecks = scheduleChecks;
    private readonly IRealtimeNotifier _notifier = notifier;

    protected override async Task HandleInternalAsync(
        MoveWorkOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var change = await _dbContext.RunInTransactionAsync(
            token => MoveAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
        _notifier.PushAssignmentChange(request.CompanyId, change);
    }

    private async Task<WorkOrderAssignmentChange> MoveAsync(
        MoveWorkOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var workOrder = await _dbContext.LoadForScheduleChangeAsync(
            request.CompanyId,
            request.Id,
            request.RowVersion,
            cancellationToken);
        var current = workOrder.Current;
        if (current.Status == WorkOrderStatus.Backlog)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.NotScheduled);
        }

        if (current.Status == WorkOrderStatus.Completed)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.CompletedScheduleLocked);
        }

        var assigneeId = request.AssigneeId ?? current.AssigneeId!.Value;
        var startUtc = await _scheduleChecks.EnsureAsync(
            request.CompanyId,
            assigneeId,
            request.PlannedStartLocal,
            workOrder.Estimate,
            workOrder.Id,
            cancellationToken);

        var oldAssigneeId = current.AssigneeId;
        var previousRevision = workOrder.CurrentRevision;
        workOrder.Move(assigneeId, request.PlannedStartLocal, startUtc);
        _dbContext.AddNewHistoryRows(workOrder, previousRevision);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or UniqueConstraintException)
        {
            throw new ConflictException(WorkOrderErrors.Rejections.WorkOrderChanged, exception);
        }

        var oldAssigneeUserId = oldAssigneeId is { } oldId
            ? await _dbContext.FindIdentityUserIdAsync(oldId, cancellationToken)
            : null;
        var newAssigneeUserId = await _dbContext.FindIdentityUserIdAsync(assigneeId, cancellationToken);

        return new WorkOrderAssignmentChange
        {
            WorkOrderId = workOrder.Id,
            Title = workOrder.Title,
            OldAssigneeId = oldAssigneeId,
            OldAssigneeUserId = oldAssigneeUserId,
            NewAssigneeId = assigneeId,
            NewAssigneeUserId = newAssigneeUserId,
            PlannedStartLocal = request.PlannedStartLocal,
        };
    }
}
