using HealthyFac.Auth.Application.Identities.IdentityAdmin;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.Extensions.Logging;
using HealthyFac.Core.Messaging.Employees;

namespace HealthyFac.Auth.Application.Identities.RevokeIdentity;

public sealed class RevokeIdentityPreHandlerCheck : PassThroughPreHandlerCheck<EmployeeDeactivated>;

/// <summary>
/// Cuts the access of a deactivated employee. The handler disables the Keycloak user, ends the sessions of the user,
/// and then writes the revocation entry with the current time, so the gateway rejects every access token of the user.
/// An employee without an identity has nothing to cut, so the message completes.
/// </summary>
/// <remarks>
/// The calls change Keycloak and Redis, which a rollback cannot undo, so the handler follows the form
/// <b>Resume</b> (rule <b>Cancellation Tokens</b>): every step is safe to repeat. The order matters. After the
/// disable, Keycloak refuses a sign-in and a refresh, so the logout removes the last sessions and no new one can
/// start. No new token exists after that, so one entry rejects every access token of the user. The entry time is one
/// second after now, because the store keeps whole seconds and the gateway compares with "before". Every failure
/// propagates, so the inbox retries the message. A retry disables, ends the sessions, and writes the entry again,
/// and a later entry time only rejects more tokens. A failure of Redis comes after the Keycloak steps, so the user
/// is already disabled while the inbox retries.
/// </remarks>
/// <param name="identityAdmin">Disables the user and ends the sessions.</param>
/// <param name="sessionRevoker">Writes the revocation entry for the gateway.</param>
/// <param name="timeProvider">Provides the time of the entry.</param>
/// <param name="logger">Logs a skipped message.</param>
public sealed class RevokeIdentityRequestHandler(
    IIdentityAdmin identityAdmin,
    ISessionRevoker sessionRevoker,
    TimeProvider timeProvider,
    ILogger<RevokeIdentityRequestHandler> logger
) : RequestHandler<EmployeeDeactivated>
{
    private readonly IIdentityAdmin _identityAdmin = identityAdmin;
    private readonly ISessionRevoker _sessionRevoker = sessionRevoker;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger<RevokeIdentityRequestHandler> _logger = logger;

    protected override async Task HandleInternalAsync(EmployeeDeactivated request, CancellationToken cancellationToken)
    {
        if (request.IdentityUserId is not { } userId)
        {
            _logger.LogInformation(
                message: "Revocation skipped: employee {EmployeeId} has no identity.",
                request.EmployeeId);

            return;
        }

        await _identityAdmin.DisableUserAsync(userId, cancellationToken);
        await _identityAdmin.LogoutUserAsync(userId, cancellationToken);
        await _sessionRevoker.RevokeAsync(userId, _timeProvider.GetUtcNow().AddSeconds(seconds: 1), cancellationToken);
    }
}
