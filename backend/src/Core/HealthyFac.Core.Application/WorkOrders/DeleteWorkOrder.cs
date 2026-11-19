using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Persistence.SoftDelete;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record DeleteWorkOrderRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The row version that the client read. The save fails when the work order changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class DeleteWorkOrderRequestValidator : AbstractValidator<DeleteWorkOrderRequest>
{
    public DeleteWorkOrderRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderErrors.Validation.WorkOrderIdRequired);
    }
}

public sealed class DeleteWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeleteWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DeleteWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Removes a work order in one transaction. The cascade removes the loaded file links, and the save interceptor turns
/// each removal into a soft delete. The history has no bound, so the handler does not load it: the soft delete
/// helper soft-deletes it in the same transaction, and a failed save leaves it unchanged. A deleted work order frees
/// its slot, because the occupancy queries join the work orders, and the query filter hides the deleted ones.
/// </summary>
/// <param name="dbContext">Loads the work order, soft-deletes the history, and saves the removal.</param>
/// <param name="timeProvider">Provides the time of the history delete.</param>
/// <param name="currentUser">Provides the actor of the history delete.</param>
public sealed class DeleteWorkOrderRequestHandler(
    ICoreDbContext dbContext,
    TimeProvider timeProvider,
    ICurrentUserProvider currentUser
) : RequestHandler<DeleteWorkOrderRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    protected override Task HandleInternalAsync(DeleteWorkOrderRequest request, CancellationToken cancellationToken) =>
        _dbContext.RunInTransactionAsync(token => DeleteAsync(request, token), cancellationToken);

    private async Task DeleteAsync(DeleteWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var workOrder = await _dbContext.WorkOrders
                .Include(candidate => candidate.Files)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);

        _dbContext.Entry(workOrder).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        await _dbContext.WorkOrderHistories
            .Where(row => row.WorkOrderId == workOrder.Id)
            .SoftDeleteAsync(_dbContext, _timeProvider, _currentUser, cancellationToken);
        _dbContext.WorkOrders.Remove(workOrder);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(WorkOrderErrors.Rejections.WorkOrderChanged, exception);
        }
    }
}
