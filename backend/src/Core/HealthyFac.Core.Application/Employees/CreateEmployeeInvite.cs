using EntityFramework.Exceptions.Common;
using FluentValidation;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Core.Messaging.Employees;
using HealthyFac.Shared.Application.CurrentUser;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.TextInput;
using HealthyFac.Shared.Application.UseCases;
using HealthyFac.Shared.Application.UseCases.PreHandlerChecks;
using HealthyFac.Shared.Domain.Emails;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees;

public record CreateEmployeeInviteRequest : IRequest<CreateEmployeeInviteResponse>
{
    public required Guid CompanyId { get; init; }

    public required string Email
    {
        get;
        init => field = EmailAddresses.Normalize(value);
    }

    public required RoleCode RoleCode { get; init; }
    public required IReadOnlyList<Guid> DepartmentIds { get; init; }
}

public record CreateEmployeeInviteResponse
{
    public required Guid Id { get; init; }
}

public class CreateEmployeeInviteRequestValidator : AbstractValidator<CreateEmployeeInviteRequest>
{
    public CreateEmployeeInviteRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.EmailRequired)
            .EmailAddress()
            .WithMessage(EmployeeInviteErrors.Validation.EmailInvalid)
            .MaximumLength(EmployeeValidationConstants.EmailMaxLength)
            .SingleLineText();

        RuleFor(x => x.CompanyId)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.CompanyIdRequired);

        RuleFor(x => x.RoleCode)
            .NotNull()
            .WithMessage(EmployeeInviteErrors.Validation.RoleCodeRequired);

        RuleFor(x => x.DepartmentIds)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.DepartmentIdsRequired);

        RuleForEach(x => x.DepartmentIds)
            .NotEmpty()
            .WithMessage(EmployeeInviteErrors.Validation.DepartmentIdRequired);
    }
}

public sealed class CreateEmployeeInvitePreHandlerCheck(ICurrentUserProvider currentUser)
    : IPreHandlerCheck<CreateEmployeeInviteRequest>
{
    private readonly ICurrentUserProvider _currentUser = currentUser;

    public Task CheckAsync(CreateEmployeeInviteRequest request, CancellationToken cancellationToken)
    {
        var actor = _currentUser.GetActor();
        actor.EnsureCompany(request.CompanyId);
        actor.EnsureDepartments(request.DepartmentIds);

        return Task.CompletedTask;
    }
}

public class CreateEmployeeInviteRequestHandler(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse>
{
    private readonly ICoreDbContext _dbContext = dbContext;
    private readonly IInviteTokenService _tokenService = tokenService;
    private readonly IOutbox _outbox = outbox;
    private readonly TimeProvider _timeProvider = timeProvider;

    public async Task<CreateEmployeeInviteResponse> HandleAsync(
        CreateEmployeeInviteRequest request,
        CancellationToken cancellationToken = default(CancellationToken)
    )
    {
        var departmentIds = request.DepartmentIds.Distinct().ToList();

        var isEmailEmployed = await _dbContext.Employees.AnyAsync(
            employee => employee.Email == request.Email, cancellationToken);
        if (isEmailEmployed)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
        }

        var hasOpenInvite = await _dbContext.EmployeeInvites.AnyAsync(
            invite => invite.CompanyId == request.CompanyId
                && invite.Email == request.Email
                && invite.AcceptedAt == null,
            cancellationToken);
        if (hasOpenInvite)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.OpenInviteExists);
        }

        var roleId = await _dbContext.Roles
                .Where(role => role.Code == request.RoleCode)
                .Select(role => (Guid?)role.Id)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.RoleNotFound);

        var departments = await _dbContext.Departments
            .Where(department => department.CompanyId == request.CompanyId && departmentIds.Contains(department.Id))
            .ToListAsync(cancellationToken);
        if (departments.Count != departmentIds.Count)
        {
            throw new NotFoundException(EmployeeInviteErrors.Rejections.DepartmentsNotFound);
        }

        var invite = new EmployeeInvite(
            request.CompanyId,
            request.Email,
            roleId,
            departments,
            _tokenService.CreateSalt(),
            _tokenService.GetExpiresAt(_timeProvider.GetUtcNow().UtcDateTime));
        _dbContext.EmployeeInvites.Add(invite);
        _outbox.Add(
            new EmployeeInvited
            {
                InviteId = invite.Id,
                CompanyId = invite.CompanyId,
                Email = invite.Email,
            });

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.OpenInviteExists, exception);
        }

        return new CreateEmployeeInviteResponse { Id = invite.Id };
    }
}
