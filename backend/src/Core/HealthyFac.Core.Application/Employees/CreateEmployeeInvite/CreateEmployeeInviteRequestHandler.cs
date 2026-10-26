using EntityFramework.Exceptions.Common;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Core.Domain.Employees;
using HealthyFac.Core.Domain.Roles;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

namespace HealthyFac.Core.Application.Employees.CreateEmployeeInvite;

public record CreateEmployeeInviteRequest : IRequest<CreateEmployeeInviteResponse>
{
    public required Guid CompanyId { get; init; }
    public required string Email { get; init; }
    public required RoleCode RoleCode { get; init; }
    public required IReadOnlyList<Guid> DepartmentIds { get; init; }
}

public record CreateEmployeeInviteResponse
{
    public required Guid Id { get; init; }
}

public class CreateEmployeeInviteRequestHandler(
    ICoreDbContext dbContext,
    IInviteTokenService tokenService,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<CreateEmployeeInviteRequest, CreateEmployeeInviteResponse>
{
    public async Task<CreateEmployeeInviteResponse> HandleAsync(
        CreateEmployeeInviteRequest request,
        CancellationToken ct = default(CancellationToken)
    )
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var departmentIds = request.DepartmentIds.Distinct().ToList();

        var isEmailEmployed = await dbContext.Employees.AnyAsync(employee => employee.Email == email, ct);
        if (isEmailEmployed)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
        }

        var hasOpenInvite = await dbContext.EmployeeInvites.AnyAsync(
            invite => invite.CompanyId == request.CompanyId
                && invite.Email == email
                && invite.AcceptedAt == null,
            ct);
        if (hasOpenInvite)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.OpenInviteExists);
        }

        var roleId = await dbContext.Roles
                .Where(role => role.Code == request.RoleCode)
                .Select(role => (Guid?)role.Id)
                .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.RoleNotFound);

        var departments = await dbContext.Departments
            .Where(department => department.CompanyId == request.CompanyId && departmentIds.Contains(department.Id))
            .ToListAsync(ct);
        if (departments.Count != departmentIds.Count)
        {
            throw new NotFoundException(EmployeeInviteErrors.Rejections.DepartmentsNotFound);
        }

        var invite = new EmployeeInvite(
            request.CompanyId,
            email,
            roleId,
            departments,
            tokenService.CreateSalt(),
            tokenService.GetExpiresAt(timeProvider.GetUtcNow().UtcDateTime));
        dbContext.EmployeeInvites.Add(invite);
        outbox.Add(
            new EmployeeInvited
            {
                InviteId = invite.Id,
                CompanyId = invite.CompanyId,
                Email = invite.Email,
            });

        try
        {
            await dbContext.SaveChangesAsync(ct);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.OpenInviteExists, exception);
        }

        return new CreateEmployeeInviteResponse { Id = invite.Id };
    }
}
