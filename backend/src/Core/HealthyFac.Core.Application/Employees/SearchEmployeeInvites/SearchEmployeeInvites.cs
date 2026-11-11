using FluentValidation;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.Grids;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.SearchEmployeeInvites;

public record SearchEmployeeInvitesRequest : GridRequest<EmployeeInviteGridFilter, EmployeeInviteSortColumn>,
    IRequest<GridResponse<EmployeeInviteRowResponse>>
{
    public required Guid CompanyId { get; init; }
}

public record EmployeeInviteRowResponse
{
    public required Guid Id { get; init; }

    public required string Email { get; init; }

    public required RoleCode RoleCode { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public required bool IsExpired { get; init; }

    /// <summary>The row version that a change of the invite (resend, revoke) sends back.</summary>
    public required uint RowVersion { get; init; }
}

public sealed class SearchEmployeeInvitesRequestValidator
    : GridRequestValidator<SearchEmployeeInvitesRequest, EmployeeInviteGridFilter, EmployeeInviteSortColumn>
{
    public SearchEmployeeInvitesRequestValidator()
        : base(EmployeeInviteSortColumn.All)
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Filter)
            .SetValidator(new EmployeeInviteGridFilterValidator())
            .When(request => request.Filter is not null);
    }
}

public sealed class SearchEmployeeInvitesPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<SearchEmployeeInvitesRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(SearchEmployeeInvitesRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Reads one page of the invites of the company that nobody accepted yet. An expired invite stays in the list with
/// <see cref="EmployeeInviteRowResponse.IsExpired"/>, so a supervisor can resend it.
/// </summary>
/// <param name="dbContext">Reads the invites.</param>
/// <param name="timeProvider">Provides the time that decides whether an invite is expired.</param>
public sealed class SearchEmployeeInvitesRequestHandler(ICoreDbContext dbContext, TimeProvider timeProvider)
    : IRequestHandler<SearchEmployeeInvitesRequest, GridResponse<EmployeeInviteRowResponse>>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<GridResponse<EmployeeInviteRowResponse>> HandleAsync(
        SearchEmployeeInvitesRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var response = await _dbContext.EmployeeInvites
            .AsNoTracking()
            .Where(invite => invite.CompanyId == request.CompanyId && invite.AcceptedAt == null)
            .ApplyFilter(request.Filter)
            .ApplySort(request.Sort, EmployeeInviteSortMap.Instance)
            .ToGridResponseAsync(
                request.Offset,
                request.Limit,
                invite => new EmployeeInviteRowResponse
                {
                    Id = invite.Id,
                    Email = invite.Email,
                    RoleCode = invite.Role.Code,
                    ExpiresAt = invite.ExpiresAt,
                    IsExpired = invite.ExpiresAt <= now,
                    RowVersion = invite.RowVersion,
                },
                cancellationToken);

        return response;
    }
}
