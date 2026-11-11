using FluentValidation;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record RevokeEmployeeInviteRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid Id { get; init; }

    /// <summary>The row version that the client read. The save fails when the invite changed since.</summary>
    public required uint RowVersion { get; init; }
}

public class RevokeEmployeeInviteRequestValidator : AbstractValidator<RevokeEmployeeInviteRequest>
{
    public RevokeEmployeeInviteRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.Id).NotEmpty().WithMessage(EmployeeInviteErrors.Validation.InviteIdRequired);
    }
}

public sealed class RevokeEmployeeInvitePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<RevokeEmployeeInviteRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(RevokeEmployeeInviteRequest request, CancellationToken cancellationToken)
    {
        _currentUser.GetActor().EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Revokes an open invite. The save interceptor turns the removal into a soft delete, so the link stops verifying
/// like an unknown link, and the open-invite index lets a new invite for the same email start. An invite of another
/// company, or a missing one, is "not found". An accepted invite stays.
/// </summary>
/// <param name="dbContext">Loads the invite and saves the removal.</param>
public sealed class RevokeEmployeeInviteRequestHandler(ICoreDbContext dbContext)
    : RequestHandler<RevokeEmployeeInviteRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;

    protected override async Task HandleInternalAsync(
        RevokeEmployeeInviteRequest request,
        CancellationToken cancellationToken
    )
    {
        var invite = await _dbContext.EmployeeInvites.SingleOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.InviteNotFound);

        if (invite.AcceptedAt is not null)
        {
            throw new UnprocessableException(EmployeeInviteErrors.Rejections.InviteAlreadyAccepted);
        }

        _dbContext.Entry(invite).Property(stored => stored.RowVersion).OriginalValue = request.RowVersion;
        _dbContext.EmployeeInvites.Remove(invite);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.InviteChanged, exception);
        }
    }
}
