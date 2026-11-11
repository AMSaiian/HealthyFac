using FluentValidation;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record DeactivateEmployeeRequest : IRequest
{
    public required Guid CompanyId { get; init; }

    public required Guid EmployeeId { get; init; }
}

public class DeactivateEmployeeRequestValidator : AbstractValidator<DeactivateEmployeeRequest>
{
    public DeactivateEmployeeRequestValidator()
    {
        RuleFor(request => request.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(request => request.EmployeeId)
            .NotEmpty()
            .WithMessage(EmployeeProvisioningErrors.Validation.EmployeeIdRequired);
    }
}

public sealed class DeactivateEmployeePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<DeactivateEmployeeRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(DeactivateEmployeeRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);

        return Task.CompletedTask;
    }
}

/// <summary>
/// Deactivates the employee and queues <see cref="EmployeeDeactivated"/> for the Auth service, which revokes the
/// sessions and disables the user. The query filters by company, so an employee of another company is not found.
/// A supervisor cannot deactivate themselves, because nobody could then undo the lockout. An employee without an
/// identity (still provisioning) gets a message without an identity ID.
/// </summary>
/// <param name="dbContext">Loads and saves the employee.</param>
/// <param name="currentUser">Reads the user who deactivates.</param>
/// <param name="outbox">Queues the message for the Auth service.</param>
/// <param name="timeProvider">Provides the time of the deactivation.</param>
public sealed class DeactivateEmployeeRequestHandler(
    ICoreDbContext dbContext,
    ICurrentUserProvider currentUser,
    IOutbox outbox,
    TimeProvider timeProvider
) : RequestHandler<DeactivateEmployeeRequest>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly ICurrentUserProvider _currentUser = currentUser;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    protected override async Task HandleInternalAsync(
        DeactivateEmployeeRequest request,
        CancellationToken cancellationToken
    )
    {
        var employee = await _dbContext.Employees.SingleOrDefaultAsync(
                employee => employee.Id == request.EmployeeId && employee.CompanyId == request.CompanyId,
                cancellationToken)
            ?? throw new NotFoundException(EmployeeProvisioningErrors.Rejections.EmployeeNotFound);

        if (employee.IsLinkedTo(_currentUser.TryGetUserId()))
        {
            throw new UnprocessableException(EmployeeProvisioningErrors.Rejections.EmployeeIsSelf);
        }

        if (!employee.CanDeactivate)
        {
            throw new UnprocessableException(EmployeeProvisioningErrors.Rejections.EmployeeAlreadyDeactivated);
        }

        var deactivatedAt = _timeProvider.GetUtcNow().UtcDateTime;
        employee.Deactivate();
        _outbox.Add(
            new EmployeeDeactivated
            {
                EmployeeId = employee.Id,
                CompanyId = employee.CompanyId,
                IdentityUserId = employee.IdentityUserId,
                DeactivatedAt = deactivatedAt,
            });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConflictException(EmployeeProvisioningErrors.Rejections.EmployeeChanged, exception);
        }
    }
}
