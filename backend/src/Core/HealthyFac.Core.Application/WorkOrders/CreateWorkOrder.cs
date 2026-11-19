using System.Data;
using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Domain.WorkOrders;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.RichText;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;

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
/// may not set an assignee or a start. The file links are created in the same save. The create rules are in
/// <see cref="WorkOrderCreation"/>.
/// </summary>
/// <param name="dbContext">Saves the work order.</param>
/// <param name="creation">Runs the create rules and adds the work order.</param>
public sealed class CreateWorkOrderRequestHandler(ICoreDbContext dbContext, WorkOrderCreation creation)
    : IRequestHandler<CreateWorkOrderRequest, CreateWorkOrderResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly WorkOrderCreation _creation = creation;

    public async Task<CreateWorkOrderResponse> HandleAsync(
        CreateWorkOrderRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        await _creation.EnsureOwnCreateAsync(request, cancellationToken);

        var workOrderId = request.AssigneeId.HasValue
            ? await _dbContext.RunInTransactionAsync(
                token => CreateAndSaveAsync(request, token),
                IsolationLevel.Serializable,
                cancellationToken)
            : await CreateAndSaveAsync(request, cancellationToken);

        return new CreateWorkOrderResponse { Id = workOrderId };
    }

    private async Task<Guid> CreateAndSaveAsync(CreateWorkOrderRequest request, CancellationToken cancellationToken)
    {
        var workOrderId = await _creation.CreateAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return workOrderId;
    }
}
