using FluentValidation;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record RetryProvisioningRequest : IRequest<RetryProvisioningResponse>
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }
}

public record RetryProvisioningResponse
{
    public required Guid EmployeeId { get; init; }
}

public class RetryProvisioningRequestValidator : AbstractValidator<RetryProvisioningRequest>
{
    public RetryProvisioningRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);
    }
}

public sealed class RetryProvisioningPreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<RetryProvisioningRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(RetryProvisioningRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
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
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var employee = await _dbContext.Employees.Include(employee => employee.Departments)
                .SingleOrDefaultAsync(
                    employee => employee.Id == request.EmployeeId && employee.CompanyId == request.CompanyId,
                    cancellationToken)
            ?? throw new NotFoundException(EmployeeProvisioningErrors.Rejections.EmployeeNotFound);

        if (!employee.CanRetryProvisioning)
        {
            throw new ConflictException(EmployeeProvisioningErrors.Rejections.EmployeeNotRetryable);
        }

        var roleCode = await _dbContext.Roles
                .Where(role => role.Id == employee.RoleId)
                .Select(role => role.Code)
                .SingleOrDefaultAsync(cancellationToken)
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
                DepartmentIds = employee.Departments.Select(department => department.Id).ToList(),
            });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmployeeProvisioningErrors.Rejections.EmployeeChanged, exception);
        }

        return new RetryProvisioningResponse { EmployeeId = employee.Id };
    }
}
