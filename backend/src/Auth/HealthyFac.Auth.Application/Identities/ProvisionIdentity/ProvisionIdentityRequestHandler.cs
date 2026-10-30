using HealthyFac.Auth.Application.Identities.Messages;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Auth.Application.Identities.ProvisionIdentity;

/// <summary>
/// Creates the identity of an accepted employee in the identity provider and reports the result to the core
/// service. The handler finds the organization of the company, finds or creates the user, adds the user to the
/// organization, maps the realm role, and emails a set-password link while the user has no password. It then
/// saves the result message through the outbox.
/// </summary>
/// <remarks>
/// The calls to the identity provider change a system that a rollback cannot undo, so the handler follows the
/// form <b>Resume</b> (rule <b>Cancellation Tokens</b>): every step is safe to repeat, and the next attempt
/// continues from the state in the provider. It finds the user by the employee ID before it creates one, and it
/// decides on the email by the missing password, never by "this call created the user". A stop or a crash after
/// the create and before the email would otherwise leave a user who never gets the link. When a parallel run for
/// the same employee creates the user first, the create fails with a conflict, and the handler continues with the
/// user that this employee ID now finds. A retry before the user sets a password sends the email again, which is
/// accepted. Three results are final: a missing organization, a used email, and a missing realm role. The handler
/// publishes each as <see cref="IdentityProvisioningFailed"/>. After an admin adds the role, a supervisor retries
/// provisioning, and the handler continues from the state in Keycloak. The handler publishes
/// <c>EmailAlreadyRegistered</c> when the create conflicts and no user of this employee exists, because another
/// user holds the email and only a person can resolve it (delete or fix that user in Keycloak, then retry
/// provisioning). Every other failure propagates, so the inbox retries the message.
/// </remarks>
/// <param name="identityAdmin">Calls the identity provider.</param>
/// <param name="outbox">Queues the result message for the core service.</param>
/// <param name="dbContext">Saves the outbox row.</param>
/// <param name="logger">Logs a failed provisioning.</param>
public sealed class ProvisionIdentityRequestHandler(
    IIdentityAdmin identityAdmin,
    IOutbox outbox,
    IAuthDbContext dbContext,
    ILogger<ProvisionIdentityRequestHandler> logger
) : RequestHandler<EmployeeAccepted>
{
    private readonly IIdentityAdmin _identityAdmin = identityAdmin;
    private readonly IOutbox _outbox = outbox;
    private readonly IAuthDbContext _dbContext = dbContext;
    private readonly ILogger<ProvisionIdentityRequestHandler> _logger = logger;

    protected override async Task HandleInternalAsync(EmployeeAccepted request, CancellationToken ct)
    {
        var organizationId = await _identityAdmin.FindOrganizationIdAsync(request.CompanyId, ct);
        if (organizationId is null)
        {
            await PublishFailedAsync(request, IdentityProvisioningFailureReasons.OrganizationNotFound, ct);

            return;
        }

        Guid userId;
        try
        {
            userId = await FindOrCreateUserAsync(request, ct);
        }
        catch (IdentityConflictException)
        {
            await PublishFailedAsync(request, IdentityProvisioningFailureReasons.EmailAlreadyRegistered, ct);

            return;
        }

        await _identityAdmin.AddOrganizationMemberAsync(organizationId.Value, userId, ct);

        try
        {
            await _identityAdmin.AssignRealmRoleAsync(userId, request.RoleCode, ct);
        }
        catch (RealmRoleNotFoundException)
        {
            await PublishFailedAsync(request, IdentityProvisioningFailureReasons.RoleNotFound, ct);

            return;
        }

        var hasPassword = await _identityAdmin.HasPasswordAsync(userId, ct);
        if (!hasPassword)
        {
            await _identityAdmin.SendSetPasswordEmailAsync(userId, ct);
        }

        _outbox.Add(new IdentityProvisioned { EmployeeId = request.EmployeeId, IdentityUserId = userId });
        await _dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Finds the user by the employee ID, else creates it. When the create conflicts with a user of a parallel run
    /// for the same employee, the method continues with that user.
    /// </summary>
    /// <param name="request">The accepted employee.</param>
    /// <param name="ct">The <see cref="CancellationToken"/> to observe while waiting.</param>
    /// <returns>The ID of the user that holds the employee ID.</returns>
    /// <exception cref="IdentityConflictException">
    /// Another user holds the email, and no user has the employee ID.
    /// </exception>
    private async Task<Guid> FindOrCreateUserAsync(EmployeeAccepted request, CancellationToken ct)
    {
        var existingUserId = await _identityAdmin.FindUserIdByEmployeeIdAsync(request.EmployeeId, ct);
        if (existingUserId is { } foundUserId)
        {
            return foundUserId;
        }

        try
        {
            return await _identityAdmin.CreateUserAsync(
                new NewIdentity
                {
                    EmployeeId = request.EmployeeId,
                    Email = request.Email,
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                },
                ct);
        }
        catch (IdentityConflictException)
        {
            var parallelUserId = await _identityAdmin.FindUserIdByEmployeeIdAsync(request.EmployeeId, ct);
            if (parallelUserId is { } createdUserId)
            {
                return createdUserId;
            }

            throw;
        }
    }

    private async Task PublishFailedAsync(EmployeeAccepted request, string reason, CancellationToken ct)
    {
        _logger.LogWarning(
            "Provisioning of employee {EmployeeId} failed with reason {Reason}.",
            request.EmployeeId,
            reason);

        _outbox.Add(new IdentityProvisioningFailed { EmployeeId = request.EmployeeId, Reason = reason });
        await _dbContext.SaveChangesAsync(ct);
    }
}
