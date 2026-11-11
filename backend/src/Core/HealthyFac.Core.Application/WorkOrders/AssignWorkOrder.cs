using System.Data;
using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record AssignWorkOrderRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    public required Guid AssigneeId { get; init; }

    /// <summary>The planned start as a time in the company zone.</summary>
    public required DateTime PlannedStartLocal { get; init; }

    /// <summary>The row version that the client read. The save fails when the work order changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class AssignWorkOrderRequestValidator : AbstractValidator<AssignWorkOrderRequest>
{
    public AssignWorkOrderRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);

        RuleFor(request => request.AssigneeId).NotEmpty().WithMessage(WorkOrderErrors.Validation.AssigneeIdRequired);
    }
}

public sealed class AssignWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<AssignWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(AssignWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Assigns a backlog work order: the four schedule rules run in order, and the new history row has the status staged.
/// The whole change is one serializable transaction, so two parallel assigns into overlapping slots of one employee
/// cannot both succeed: the loser runs again, sees the slot of the winner, and gets the rule rejection.
/// </summary>
/// <param name="dbContext">Loads and saves the work order.</param>
/// <param name="scheduleChecks">Checks the slot.</param>
public sealed class AssignWorkOrderRequestHandler(ICoreDbContext dbContext, ScheduleChecks scheduleChecks)
    : RequestHandler<AssignWorkOrderRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ScheduleChecks _scheduleChecks = scheduleChecks;

    protected override async Task HandleInternalAsync(
        AssignWorkOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        await _dbContext.RunInTransactionAsync(
            token => AssignAsync(request, token),
            IsolationLevel.Serializable,
            cancellationToken);
    }

    private async Task AssignAsync(AssignWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var workOrder = await _dbContext.LoadForScheduleChangeAsync(
            request.CompanyId,
            request.Id,
            request.RowVersion,
            cancellationToken);
        if (workOrder.Current.Status != WorkOrderStatus.Backlog)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.NotInBacklog);
        }

        var startUtc = await _scheduleChecks.EnsureAsync(
            request.CompanyId,
            request.AssigneeId,
            request.PlannedStartLocal,
            workOrder.Estimate,
            workOrder.Id,
            cancellationToken);

        var previousRevision = workOrder.CurrentRevision;
        workOrder.Assign(request.AssigneeId, request.PlannedStartLocal, startUtc);
        _dbContext.AddNewHistoryRows(workOrder, previousRevision);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (Exception exception) when (exception is DbUpdateConcurrencyException or UniqueConstraintException)
        {
            throw new ConflictException(WorkOrderErrors.Rejections.WorkOrderChanged, exception);
        }
    }
}
