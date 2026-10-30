using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.RetryProvisioning;

public record RetryProvisioningRequest : IRequest<RetryProvisioningResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }
}

public record RetryProvisioningResponse
{
    public required Guid EmployeeId { get; init; }
}

/// <summary>
/// Sends a new <see cref="EmployeeAccepted"/> message for an employee who waits for an identity. The employee is
/// failed or still provisioning, because the Auth inbox can give up on a message for good and leave the employee
/// provisioning. The Auth consumer is idempotent, so a second provisioning is harmless. The query filters by
/// company, so an employee of another company is not found. Every retry changes the employee row, so a parallel
/// change (for example a deactivation) gives <c>409</c> through the <c>EmployeeChanged</c> conflict.
/// </summary>
/// <param name="dbContext">Loads and saves the employee.</param>
/// <param name="outbox">Queues the message for the Auth service.</param>
/// <param name="timeProvider">Provides the time of the retry.</param>
public sealed class RetryProvisioningRequestHandler(ICoreDbContext dbContext, IOutbox outbox, TimeProvider timeProvider)
    : IRequestHandler<RetryProvisioningRequest, RetryProvisioningResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<RetryProvisioningResponse> HandleAsync(
        RetryProvisioningRequest request,
        CancellationToken ct = default(CancellationToken)
    )
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(
                employee => employee.Id == request.EmployeeId && employee.CompanyId == request.CompanyId,
                ct)
            ?? throw new NotFoundException(EmployeeProvisioningErrors.Rejections.EmployeeNotFound);

        if (!employee.CanRetryProvisioning)
        {
            throw new ConflictException(EmployeeProvisioningErrors.Rejections.EmployeeNotRetryable);
        }

        var roleCode = await _dbContext.Roles
                .Where(role => role.Id == employee.RoleId)
                .Select(role => role.Code)
                .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.RoleNotFound);

        employee.RetryProvisioning(_timeProvider.GetUtcNow().UtcDateTime);
        _outbox.Add(
            new EmployeeAccepted
            {
                EmployeeId = employee.Id,
                CompanyId = employee.CompanyId,
                Email = employee.Email,
                FirstName = employee.FirstName,
                LastName = employee.LastName,
                RoleCode = roleCode.Value,
            });

        try
        {
            await _dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmployeeProvisioningErrors.Rejections.EmployeeChanged, exception);
        }

        return new RetryProvisioningResponse { EmployeeId = employee.Id };
    }
}
