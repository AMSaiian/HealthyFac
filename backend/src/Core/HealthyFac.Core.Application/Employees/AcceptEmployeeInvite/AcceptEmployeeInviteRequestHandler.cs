using EntityFramework.Exceptions.Common;
using HealthyFac.Core.Application.Employees.InviteTokens;
using HealthyFac.Core.Application.Employees.Messages;
using HealthyFac.Shared.Application.ErrorHandling;
using HealthyFac.Shared.Application.Logging;
using HealthyFac.Shared.Application.Messaging;
using HealthyFac.Shared.Application.UseCases;
using Microsoft.EntityFrameworkCore;

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

public class AcceptEmployeeInviteRequestHandler(
    ICoreDbContext dbContext,
    EmployeeInviteResolver inviteResolver,
    IOutbox outbox,
    TimeProvider timeProvider
) : IRequestHandler<AcceptEmployeeInviteRequest, AcceptEmployeeInviteResponse>
{
    public async Task<AcceptEmployeeInviteResponse> HandleAsync(
        AcceptEmployeeInviteRequest request,
        CancellationToken ct = default(CancellationToken)
    )
    {
        var invite = await inviteResolver.GetAcceptableAsync(request.Token, ct);
        var isEmailEmployed = await dbContext.Employees.AnyAsync(employee => employee.Email == invite.Email, ct);
        if (isEmailEmployed)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed);
        }

        var roleCode = await dbContext.Roles
                .Where(role => role.Id == invite.RoleId)
                .Select(role => role.Code)
                .SingleOrDefaultAsync(ct)
            ?? throw new NotFoundException(EmployeeInviteErrors.Rejections.RoleNotFound);

        invite.UpdateProfile(request.FirstName, request.LastName);
        var employee = invite.Accept(timeProvider.GetUtcNow().UtcDateTime);
        dbContext.Employees.Add(employee);
        outbox.Add(
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
            await dbContext.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new NotFoundException(EmployeeInviteErrors.Rejections.InviteLinkInvalid, exception);
        }
        catch (UniqueConstraintException exception)
        {
            throw new ConflictException(EmployeeInviteErrors.Rejections.EmailAlreadyEmployed, exception);
        }

        return new AcceptEmployeeInviteResponse { EmployeeId = employee.Id, };
    }
}
