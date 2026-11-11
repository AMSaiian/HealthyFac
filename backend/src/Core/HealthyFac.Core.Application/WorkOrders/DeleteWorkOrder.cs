using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
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
/// Removes a work order and its file links in one save. The save interceptor turns each removal into a soft delete.
/// The history rows stay unchanged (they are append-only). A deleted work order frees its slot, because the occupancy
/// queries join the work orders, and the query filter hides the deleted ones.
/// </summary>
/// <param name="dbContext">Loads the work order and saves the removal.</param>
public sealed class DeleteWorkOrderRequestHandler(ICoreDbContext dbContext) : RequestHandler<DeleteWorkOrderRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        DeleteWorkOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        var workOrder = await _dbContext.WorkOrders
                .Include(candidate => candidate.Files)
                .SingleOrDefaultAsync(
                    candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(WorkOrderErrors.Rejections.WorkOrderNotFound);

        _dbContext.Entry(workOrder).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        _dbContext.WorkOrderFiles.RemoveRange(workOrder.Files);
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
