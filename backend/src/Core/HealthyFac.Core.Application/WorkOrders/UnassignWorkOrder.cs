using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record UnassignWorkOrderRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The row version that the client read. The save fails when the work order changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class UnassignWorkOrderRequestValidator : AbstractValidator<UnassignWorkOrderRequest>
{
    public UnassignWorkOrderRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);
    }
}

public sealed class UnassignWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<UnassignWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(UnassignWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Sends a scheduled work order back to the backlog: the new history row has no assignee and no planned start. It frees
/// the slot, so it needs no schedule rule and one plain save.
/// </summary>
/// <param name="dbContext">Loads and saves the work order.</param>
public sealed class UnassignWorkOrderRequestHandler(ICoreDbContext dbContext)
    : RequestHandler<UnassignWorkOrderRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        UnassignWorkOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var workOrder = await _dbContext.LoadForScheduleChangeAsync(
            request.CompanyId,
            request.Id,
            request.RowVersion,
            cancellationToken);
        if (workOrder.Current.Status == WorkOrderStatus.Backlog)
        {
            throw new UnprocessableException(WorkOrderErrors.Rejections.NotScheduled);
        }

        var previousRevision = workOrder.CurrentRevision;
        workOrder.Unassign();
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
