using FluentValidation;
using HealthyFac.Core.Application.Authorization;
using HealthyFac.Core.Application.Lookups;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.CurrentUser.Actors;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.WorkOrders;

public record LookupWorkOrdersRequest : IRequest<LookupResponse>
{
    public required Guid CompanyId { get; init; }

    /// <summary>The scope of the actor. The Web layer sets it from the token.</summary>
    public required AccessScope Scope { get; init; }

    /// <summary>The start of the title. Without it the lookup answers the first work orders by title.</summary>
    public string? Search { get; init; }
}

public sealed class LookupWorkOrdersRequestValidator : AbstractValidator<LookupWorkOrdersRequest>
{
    public LookupWorkOrdersRequestValidator()
    {
        RuleFor(request => request.CompanyId).NotEmpty().WithMessage(LookupErrors.CompanyIdRequired);

        RuleFor(request => request.Search)
            .MaximumLength(LookupValidationConstants.SearchMaxLength)
            .WithMessage(LookupErrors.SearchTooLong)
            .SingleLineText();
    }
}

public sealed class LookupWorkOrdersPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<LookupWorkOrdersRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(LookupWorkOrdersRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads the ID and the title of the work orders that the access scope reaches, whose title starts with the search
/// text, at most <see cref="LookupValidationConstants.MaxItems"/>, sorted by title. A deleted work order does not exist
/// any more, so it is never offered.
/// </summary>
/// <param name="dbContext">Reads the work orders.</param>
/// <param name="ownEmployee">Finds the employee of the actor for the own scope.</param>
/// <param name="currentUser">Provides the user of the actor.</param>
public sealed class LookupWorkOrdersRequestHandler(
    ICoreDbContext dbContext,
    OwnEmployeeResolver ownEmployee,
    ICurrentUserProvider currentUser
) : IRequestHandler<LookupWorkOrdersRequest, LookupResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly OwnEmployeeResolver _ownEmployee = ownEmployee;
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public async Task<LookupResponse> HandleAsync(
        LookupWorkOrdersRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var ownEmployeeId = request.Scope == AccessScope.Own
            ? await _ownEmployee.FindIdAsync(cancellationToken)
            : null;

        var query = _dbContext.WorkOrders
            .AsNoTracking()
            .ApplyScope(request.Scope, request.CompanyId, ownEmployeeId, _currentUser.GetUserId());

        if (!string.IsNullOrEmpty(request.Search))
        {
            query = query.Where(workOrder => workOrder.Title.StartsWith(request.Search));
        }

        var items = await query
            .OrderBy(workOrder => workOrder.Title)
            .ThenBy(workOrder => workOrder.Id)
            .Take(LookupValidationConstants.MaxItems)
            .Select(workOrder => new LookupOption
                { Id = workOrder.Id, DisplayName = workOrder.Title, IsDeleted = false })
            .ToListAsync(cancellationToken);

        return new LookupResponse { Items = items };
    }
}
