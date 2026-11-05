using HealthyFac.Core.Domain.Employees;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HealthyFac.Core.Application.Employees.FailProvisioning;

public sealed class FailProvisioningPreHandlerCheck : PassThroughPreHandlerCheck<IdentityProvisioningFailed>;

/// <summary>
/// Marks the provisioning of the employee as failed and logs the reason. An employee who already failed completes
/// without a change, because the same result can arrive twice. An active employee also completes without a change,
/// because the identity works and the failure is a late message of a parallel run. A deactivated employee completes
/// without a change too, because nobody needs the identity anymore. Every state is handled, so the message never
/// retries.
/// </summary>
/// <param name="dbContext">Loads and saves the employee.</param>
/// <param name="logger">Logs the reason of the failure.</param>
public sealed class FailProvisioningRequestHandler(
    ICoreDbContext dbContext,
    ILogger<FailProvisioningRequestHandler> logger
) : RequestHandler<IdentityProvisioningFailed>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ILogger<FailProvisioningRequestHandler> _logger = logger;

    protected override async Task HandleInternalAsync(
        IdentityProvisioningFailed request,
        CancellationToken cancellationToken
    )
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(
                employee => employee.Id == request.EmployeeId,
                cancellationToken)
            ?? throw EmployeeProvisioningErrors.EmployeeMissing(request.EmployeeId);

        _logger.LogWarning(
            "Provisioning of employee {EmployeeId} failed with reason {Reason}.",
            request.EmployeeId,
            request.Reason);

        var isAlreadyHandled = employee.Status == EmployeeStatus.ProvisioningFailed
            || employee.Status == EmployeeStatus.Active
            || employee.Status == EmployeeStatus.Deactivated;
        if (isAlreadyHandled)
        {
            return;
        }

        employee.MarkProvisioningFailed();
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
