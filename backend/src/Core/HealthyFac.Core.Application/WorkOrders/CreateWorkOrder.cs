using System.Data;
using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record CreateWorkOrderRequest : IRequest<CreateWorkOrderResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required WorkOrderPriority Priority { get; init; }

    public required WorkOrderCategory Category { get; init; }

    public Guid? MachineId { get; init; }

    public required int EstimateMinutes { get; init; }

    public required IReadOnlyList<Guid> FileIds { get; init; }

    /// <summary>
    /// The assignee. Only the company scope may set it, together with <see cref="PlannedStartLocal"/>.
    /// </summary>
    public Guid? AssigneeId { get; init; }

    /// <summary>The planned start as a time in the company zone.</summary>
    public DateTime? PlannedStartLocal { get; init; }
}

public record CreateWorkOrderResponse
{
    public required Guid Id { get; init; }
}

public class CreateWorkOrderRequestValidator : AbstractValidator<CreateWorkOrderRequest>
{
    public CreateWorkOrderRequestValidator(IRichTextPolicy richTextPolicy)
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Priority).NotNull().WithMessage(WorkOrderErrors.Validation.PriorityRequired);

        RuleFor(request => request.Category).NotNull().WithMessage(WorkOrderErrors.Validation.CategoryRequired);

        RuleFor(request => request.Title).IsWorkOrderTitle();

        RuleFor(request => request.Description).IsWorkOrderDescription(richTextPolicy);

        RuleFor(request => request.EstimateMinutes).IsWorkOrderEstimate();

        RuleFor(request => request.FileIds).IsWorkOrderFileList();

        RuleFor(request => request)
            .Must(request => request.AssigneeId.HasValue == request.PlannedStartLocal.HasValue)
            .When(request => request.Scope == AccessScope.Company)
            .WithMessage(WorkOrderErrors.Validation.AssigneeAndStartTogether);
    }
}

public sealed class CreateWorkOrderPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CreateWorkOrderRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Creates a work order in the backlog. The company scope may also assign it: the four schedule rules then run in one
/// serializable transaction, and the work order is created with its first two history rows in one save. The own scope
/// may not set an assignee or a start. The file links are created in the same save.
/// </summary>
/// <param name="dbContext">Reads the machine and saves the work order.</param>
/// <param name="scheduleChecks">Checks the slot of an assigned work order.</param>
/// <param name="ownEmployee">Finds the employee of the actor: the own scope needs one.</param>
public sealed class CreateWorkOrderRequestHandler(
    ICoreDbContext dbContext,
    ScheduleChecks scheduleChecks,
    OwnEmployeeResolver ownEmployee
) : IRequestHandler<CreateWorkOrderRequest, CreateWorkOrderResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ScheduleChecks _scheduleChecks = scheduleChecks;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;

    public async Task<CreateWorkOrderResponse> HandleAsync(
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        if (request.Scope == AccessScope.Own)
        {
            await EnsureOwnCreateAsync(request, cancellationToken);
        }

        var workOrderId = request.AssigneeId.HasValue
            ? await _dbContext.RunInTransactionAsync(
                token => CreateAsync(request, token),
                IsolationLevel.Serializable,
                cancellationToken)
            : await CreateAsync(request, cancellationToken);

        return new CreateWorkOrderResponse { Id = workOrderId };
    }

    private async Task EnsureOwnCreateAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.AssigneeId.HasValue || request.PlannedStartLocal.HasValue)
        {
            throw new ForbiddenAccessException(WorkOrderErrors.Rejections.OwnScopeForbidden);
        }

        if (await _ownEmployee.FindIdAsync(cancellationToken) is null)
        {
            throw new NotFoundException(WorkOrderErrors.Rejections.OwnEmployeeNotFound);
        }
    }

    private async Task<Guid> CreateAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        if (request.MachineId is { } machineId)
        {
            var isMachineInCompany = await _dbContext.Machines
                .AsNoTracking()
                .AnyAsync(
                    machine => machine.Id == machineId && machine.CompanyId == request.CompanyId,
                    cancellationToken);
            if (!isMachineInCompany)
            {
                throw new NotFoundException(WorkOrderErrors.Rejections.MachineNotFound);
            }
        }

        var estimate = TimeSpan.FromMinutes(request.EstimateMinutes);
        var workOrder = WorkOrder.Create(
            request.CompanyId,
            request.Title,
            request.Description,
            request.Priority,
            request.Category,
            request.MachineId,
            estimate);
        workOrder.ReplaceFiles(request.FileIds);

        if (request is { AssigneeId: { } assigneeId, PlannedStartLocal: { } plannedStartLocal })
        {
            var startUtc = await _scheduleChecks.EnsureAsync(
                request.CompanyId,
                assigneeId,
                plannedStartLocal,
                estimate,
                workOrder.Id,
                cancellationToken);
            workOrder.Assign(assigneeId, plannedStartLocal, startUtc);
        }

        _dbContext.WorkOrders.Add(workOrder);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return workOrder.Id;
    }
}
