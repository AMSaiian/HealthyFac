using HealthyFac.Core.Application.Employees.DeactivateEmployee;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.CompleteProvisioning;

public sealed class CompleteProvisioningPreHandlerCheck : PassThroughPreHandlerCheck<IdentityProvisioned>;

/// <summary>
/// Activates the employee and links the identity that the Auth service created. An active employee with the same
/// identity completes without a change, because the same result can arrive twice (a retry of provisioning, or a
/// first sign-in that activated the employee first). A deactivated employee stays deactivated: the handler links
/// the new identity and queues a second <see cref="EmployeeDeactivated"/> with it, so the Auth service revokes,
/// disables, and logs out the user that it created after the deactivation. A deactivated employee with the same
/// identity completes without a change. Any other state, or another identity, throws, so the inbox retries the
/// message and finally marks the row failed with an <c>Error</c> log entry.
/// </summary>
/// <param name="dbContext">Loads and saves the employee.</param>
/// <param name="outbox">Queues the second deactivation message.</param>
/// <param name="timeProvider">Provides the time of the second deactivation.</param>
public sealed class CompleteProvisioningRequestHandler(
    ICoreDbContext dbContext,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<IdentityProvisioned>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(IdentityProvisioned request, CancellationToken cancellationToken)
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(
                employee => employee.Id == request.EmployeeId,
                cancellationToken)
            ?? throw EmployeeProvisioningErrors.EmployeeMissing(request.EmployeeId);

        var isAlreadyCompleted = employee.IsLinkedTo(request.IdentityUserId)
            && (employee.Status == EmployeeStatus.Active || employee.Status == EmployeeStatus.Deactivated);
        if (isAlreadyCompleted)
        {
            return;
        }

        if (employee.Status == EmployeeStatus.Deactivated)
        {
            employee.LinkIdentity(request.IdentityUserId);
            _outbox.Add(
                new EmployeeDeactivated
                {
                    EmployeeId = employee.Id,
                    CompanyId = employee.CompanyId,
                    IdentityUserId = request.IdentityUserId,
                    DeactivatedAt = _timeProvider.GetUtcNow().UtcDateTime,
                });
        }
        else
        {
            employee.Activate(request.IdentityUserId);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
