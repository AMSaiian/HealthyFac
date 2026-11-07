using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using Microsoft.EntityFrameworkCore;
using HealthyFac.Core.Messaging.Employees;

namespace HealthyFac.Core.Application.Employees.AcceptEmployeeInvite;

public record AcceptEmployeeInviteRequest : IRequest<AcceptEmployeeInviteResponse>
{
    [SensitiveData]
    public required string Token { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }
}

public record AcceptEmployeeInviteResponse
{
    public required Guid EmployeeId { get; init; }
}

public class AcceptEmployeeInviteRequestValidator : AbstractValidator<AcceptEmployeeInviteRequest>
{
    public AcceptEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.TokenRequired)
            .SingleLineText();

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.FirstNameRequired)
            .MaximumLength(EmployeeValidationConstants.NameMaxLength)
            .SingleLineText();

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.LastNameRequired)
            .MaximumLength(EmployeeValidationConstants.NameMaxLength)
            .SingleLineText();
    }
}

public sealed class AcceptEmployeeInvitePreHandlerCheck : PassThroughPreHandlerCheck<AcceptEmployeeInviteRequest>;

public class AcceptEmployeeInviteRequestHandler(
    ICoreDbContext dbContext,
    EmployeeInviteResolver inviteResolver,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly EmployeeInviteResolver _inviteResolver = inviteResolver;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<AcceptEmployeeInviteResponse> HandleAsync(
        AcceptEmployeeInviteRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var invite = await _inviteResolver.GetAcceptableAsync(request.Token, cancellationToken);
        var isEmailEmployed = await _dbContext.Employees.AnyAsync(
            employee => employee.Email == invite.Email, cancellationToken);
        if (isEmailEmployed)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
        }

        var roleCode = await _dbContext.Roles
                .Where(role => role.Id == invite.RoleId)
                .Select(role => role.Code)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.RoleNotFound);

        invite.UpdateProfile(request.FirstName, request.LastName);
        var employee = invite.Accept(_timeProvider.GetUtcNow().UtcDateTime);
        _dbContext.Employees.Add(employee);
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
            throw new NotFoundException(EmployeeInviteErrors.Rejections.InviteLinkInvalid, exception);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed, exception);
        }

        return new AcceptEmployeeInviteResponse { EmployeeId = employee.Id };
    }
}
