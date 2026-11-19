using FluentValidation;
using HealthyFac.Core.Application.Machines;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record ListOpenMachineWorkOrdersRequest : IRequest<IReadOnlyList<OpenWorkOrderResponse>>
{
    public required Guid CompanyId { get; init; }

    public required Guid MachineId { get; init; }

    public required int Limit { get; init; }
}

/// <summary>
/// One open work order of a machine. It holds no assignee and no employee name, so a reader of the list learns
/// nothing about people.
/// </summary>
public record OpenWorkOrderResponse
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public required WorkOrderStatus Status { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public DateTime? PlannedStartUtc { get; init; }

    /// <summary>The planned start plus the estimate. It is set only with a planned start.</summary>
    public DateTime? PlannedEndUtc { get; init; }
}

public class ListOpenMachineWorkOrdersRequestValidator : AbstractValidator<ListOpenMachineWorkOrdersRequest>
{
    public ListOpenMachineWorkOrdersRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.MachineId).NotEmpty().WithMessage(WorkOrderErrors.Validation.MachineIdRequired);

        RuleFor(request => request.Limit)
            .InclusiveBetween(GridValidationConstants.MinLimit, GridValidationConstants.MaxLimit)
            .WithMessage(GridErrors.Validation.LimitOutOfRange);
    }
}

public sealed class ListOpenMachineWorkOrdersPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<ListOpenMachineWorkOrdersRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(ListOpenMachineWorkOrdersRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Lists the work orders of a machine whose current status is not <c>completed</c> or <c>rejected</c>, newest first.
/// The answer covers every work order of the company, whatever the assignee, because the reader is a service that plans
/// work and never shows a person.
/// </summary>
/// <param name="dbContext">Reads the machine and its work orders.</param>
public sealed class ListOpenMachineWorkOrdersRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<ListOpenMachineWorkOrdersRequest, IReadOnlyList<OpenWorkOrderResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<IReadOnlyList<OpenWorkOrderResponse>> HandleAsync(
        ListOpenMachineWorkOrdersRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var isMachineKnown = await _dbContext.Machines.AnyAsync(
            machine => machine.Id == request.MachineId && machine.CompanyId == request.CompanyId,
            cancellationToken);

        if (!isMachineKnown)
        {
            throw new NotFoundException(MachineErrors.MachineNotFound);
        }

        var rows = await _dbContext.WorkOrders
            .AsNoTracking()
            .Where(workOrder => workOrder.CompanyId == request.CompanyId && workOrder.MachineId == request.MachineId)
            .Select(workOrder => new
            {
                workOrder.Id,
                workOrder.Title,
                workOrder.Priority,
                workOrder.Category,
                workOrder.Estimate,
                workOrder.CreatedAt,
                Current = workOrder.History.First(history => history.Revision == workOrder.CurrentRevision),
            })
            .Where(row => row.Current.Status != WorkOrderStatus.Completed
                && row.Current.Status != WorkOrderStatus.Rejected)
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.Id)
            .Take(request.Limit)
            .ToListAsync(cancellationToken);

        return rows
            .Select(row => new OpenWorkOrderResponse
            {
                Id = row.Id,
                Title = row.Title,
                Status = row.Current.Status,
                Priority = row.Priority,
                Category = row.Category,
                PlannedStartUtc = row.Current.PlannedStartUtc,
                PlannedEndUtc = row.Current.PlannedStartUtc?.Add(row.Estimate),
            })
            .ToList();
    }
}
