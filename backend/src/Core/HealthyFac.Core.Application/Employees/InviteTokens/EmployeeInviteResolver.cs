using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Application.ErrorHandling;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.InviteTokens;

public class EmployeeInviteResolver(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    TimeProvider timeProvider
)
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IInviteTokenService _tokenService = tokenService;
    private readonly TimeProvider _timeProvider = timeProvider;

    /// <summary>
    /// Finds the invite that a token names and checks that the invite is still open. Every failure throws the same
    /// exception on purpose, so a caller without a valid link cannot learn which check failed.
    /// </summary>
    /// <param name="token">The token from the invite link.</param>
    /// <param name="cancellationToken">Cancels the database queries.</param>
    /// <returns>The tracked invite with its <see cref="EmployeeInvite.Departments"/> loaded.</returns>
    /// <exception cref="NotFoundException">
    /// Thrown with <see cref="EmployeeInviteErrors.Rejections.InviteLinkInvalid"/> when the token is malformed, the
    /// invite does not exist, the signature does not match the salt of the invite, or the invite is accepted or
    /// expired.
    /// </exception>
    public async Task<EmployeeInvite> GetAcceptableAsync(string token, CancellationToken cancellationToken)
    {
        if (!_tokenService.TryReadInviteId(token, out var inviteId))
        {
            throw CreateInvalidLinkException();
        }

        var invite = await _dbContext.EmployeeInvites
            .Include(invite => invite.Departments)
            .SingleOrDefaultAsync(invite => invite.Id == inviteId, cancellationToken);

        if (invite is null
            || !_tokenService.IsValid(token, invite.TokenSalt)
            || !invite.CanAccept(_timeProvider.GetUtcNow().UtcDateTime))
        {
            throw CreateInvalidLinkException();
        }

        return invite;
    }

    private static NotFoundException CreateInvalidLinkException() =>
        new(EmployeeInviteErrors.Rejections.InviteLinkInvalid);
}
