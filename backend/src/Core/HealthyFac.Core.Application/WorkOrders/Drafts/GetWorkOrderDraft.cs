using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders.Drafts;

public record GetWorkOrderDraftRequest : IRequest<WorkOrderDraftResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }
}

public class GetWorkOrderDraftRequestValidator : AbstractValidator<GetWorkOrderDraftRequest>
{
    public GetWorkOrderDraftRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.CompanyIdRequired);
        RuleFor(request => request.Id).NotEmpty().WithMessage(WorkOrderDraftErrors.Validation.DraftIdRequired);
    }
}

public sealed class GetWorkOrderDraftPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<GetWorkOrderDraftRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(GetWorkOrderDraftRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one work order draft of the company. A draft of another company is "not found".
/// </summary>
/// <param name="dbContext">Reads the draft.</param>
public sealed class GetWorkOrderDraftRequestHandler(ICoreDbContext dbContext)
    : IRequestHandler<GetWorkOrderDraftRequest, WorkOrderDraftResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    public async Task<WorkOrderDraftResponse> HandleAsync(
        GetWorkOrderDraftRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var response = await _dbContext.WorkOrderDrafts
            .AsNoTracking()
            .Where(draft => draft.Id == request.Id && draft.CompanyId == request.CompanyId)
            .Select(WorkOrderDraftResponse.ProjectionFor(_dbContext))
            .SingleOrDefaultAsync(cancellationToken);

        return response ?? throw new NotFoundException(WorkOrderDraftErrors.DraftNotFound);
    }
}
